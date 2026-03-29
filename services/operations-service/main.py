from __future__ import annotations

from typing import Any

from fastapi import Depends, FastAPI, Header, HTTPException
from pydantic_settings import BaseSettings, SettingsConfigDict

from erpnext_client import ERPNextClient, parse_erpnext_datetime
from models import (
    CardKind,
    CardSummary,
    CardTransitionRequest,
    CardTransitionResult,
    ErrorResponse,
    GenericKanbanBoard,
    HealthStatus,
    JobCardDetail,
    KanbanBoard,
    KanbanColumn,
    OrderKanbanBoard,
    OrderTasksResponse,
    SalesOrderSummary,
    TaskSummary,
    TaskLikeKind,
    TransitionRequest,
    TransitionResult,
    WebhookAck,
)
from workflow import JOB_CARD_WORKFLOW, TASK_WORKFLOW, WORK_ORDER_WORKFLOW


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    erp_base_url: str = "http://localhost:8080"
    erp_api_key: str | None = None
    erp_api_secret: str | None = None

    webhook_token: str | None = None

    service_port: int = 8003


settings = Settings()

app = FastAPI(title="operations-service", version="0.1.0")


def _resolve_workflow(kind: CardKind):
    if kind == "task":
        return TASK_WORKFLOW
    if kind == "work_order":
        return WORK_ORDER_WORKFLOW
    if kind == "job_card":
        return JOB_CARD_WORKFLOW
    # Defensive fallback
    return TASK_WORKFLOW


def _bucket_status(workflow, status: str) -> str:
    keys = {k for (k, _) in workflow.columns}
    return status if status in keys else "Other"


def _card_title(kind: CardKind, raw: dict[str, Any]) -> str | None:
    if kind == "task":
        return raw.get("subject")
    if kind == "work_order":
        # production_item is usually the clearest human label
        return raw.get("production_item")
    if kind == "job_card":
        op = raw.get("operation")
        wo = raw.get("work_order")
        if op and wo:
            return f"{op} ({wo})"
        return op or wo
    return None


def _to_card(kind: CardKind, raw: dict[str, Any]) -> CardSummary | None:
    name = raw.get("name")
    if not name:
        return None
    status = str(raw.get("status") or "")
    return CardSummary(
        kind=kind,
        name=str(name),
        title=_card_title(kind, raw),
        status=status,
        modified=parse_erpnext_datetime(raw.get("modified")),
        sales_order=raw.get("sales_order"),
        work_order=raw.get("work_order"),
        operation=raw.get("operation"),
    )


def _to_sales_order(raw: dict[str, Any]) -> SalesOrderSummary | None:
    name = raw.get("name")
    if not name:
        return None
    return SalesOrderSummary(
        name=str(name),
        customer=raw.get("customer"),
        status=raw.get("status"),
        transaction_date=raw.get("transaction_date"),
        modified=parse_erpnext_datetime(raw.get("modified")),
    )


def _to_job_card_detail(raw: dict[str, Any]) -> JobCardDetail | None:
    name = raw.get("name")
    if not name:
        return None
    for_qty = raw.get("for_quantity")
    try:
        for_qty_f = float(for_qty) if for_qty is not None else None
    except (TypeError, ValueError):
        for_qty_f = None

    return JobCardDetail(
        name=str(name),
        work_order=raw.get("work_order"),
        operation=raw.get("operation"),
        for_quantity=for_qty_f,
        modified=parse_erpnext_datetime(raw.get("modified")),
        status=raw.get("status"),
    )


def get_erp() -> ERPNextClient:
    return ERPNextClient(
        base_url=settings.erp_base_url,
        api_key=settings.erp_api_key,
        api_secret=settings.erp_api_secret,
    )


@app.get("/health")
def health() -> dict[str, HealthStatus]:
    return {"status": "ok"}


@app.get("/erp/ping")
async def erp_ping(erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    data = await erp.ping()
    return {"erp_base_url": settings.erp_base_url, "response": data}


@app.get("/kanban/board", response_model=KanbanBoard)
async def kanban_board(erp: ERPNextClient = Depends(get_erp)) -> KanbanBoard:
    columns = [KanbanColumn(key=k, title=t) for (k, t) in TASK_WORKFLOW.columns]

    tasks_by_status: dict[str, list[TaskSummary]] = {}
    for (status_key, _) in TASK_WORKFLOW.columns:
        raw = await erp.list_tasks(status=status_key, limit=50)
        tasks_by_status[status_key] = [
            TaskSummary(
                name=t.get("name", ""),
                subject=t.get("subject"),
                status=t.get("status", status_key),
                modified=parse_erpnext_datetime(t.get("modified")),
            )
            for t in raw
            if t.get("name")
        ]

    return KanbanBoard(columns=columns, tasks_by_status=tasks_by_status)


@app.post(
    "/tasks/{name}/transition",
    response_model=TransitionResult,
    responses={400: {"model": ErrorResponse}, 404: {"model": ErrorResponse}},
)
async def transition_task(
    name: str,
    req: TransitionRequest,
    erp: ERPNextClient = Depends(get_erp),
) -> TransitionResult:
    current = await erp.get_task(name)
    data = current.get("data")
    if not isinstance(data, dict):
        raise HTTPException(status_code=404, detail="Task not found")

    from_status = str(data.get("status") or "")
    to_status = req.to_status

    if not from_status:
        raise HTTPException(status_code=400, detail="Task status missing")

    if not TASK_WORKFLOW.can_transition(from_status, to_status):
        raise HTTPException(
            status_code=400,
            detail=f"Transition not allowed: {from_status} -> {to_status}",
        )

    if from_status == to_status:
        return TransitionResult(ok=True, from_status=from_status, to_status=to_status, erpnext_response=None)

    updated = await erp.update_task_status(name, to_status)
    return TransitionResult(ok=True, from_status=from_status, to_status=to_status, erpnext_response=updated)


@app.get("/timeline/tasks", response_model=list[TaskSummary])
async def timeline_tasks(erp: ERPNextClient = Depends(get_erp)) -> list[TaskSummary]:
    raw = await erp.list_tasks(status=None, limit=50)
    return [
        TaskSummary(
            name=t.get("name", ""),
            subject=t.get("subject"),
            status=t.get("status", ""),
            modified=parse_erpnext_datetime(t.get("modified")),
        )
        for t in raw
        if t.get("name")
    ]


@app.get("/kanban/generic-board", response_model=GenericKanbanBoard)
async def generic_kanban_board(
    kind: CardKind = "task",
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> GenericKanbanBoard:
    workflow = _resolve_workflow(kind)
    columns = [KanbanColumn(key=k, title=t) for (k, t) in workflow.columns]

    cards_by_status: dict[str, list[CardSummary]] = {k: [] for (k, _) in workflow.columns}
    cards_by_status.setdefault("Other", [])

    if kind == "task":
        raw_items = await erp.list_tasks(status=None, limit=limit)
    elif kind == "work_order":
        raw_items = await erp.list_work_orders(status=None, limit=limit)
    else:
        raw_items = await erp.list_job_cards(status=None, limit=limit)

    for raw in raw_items:
        card = _to_card(kind, raw)
        if card is None:
            continue
        bucket = _bucket_status(workflow, card.status)
        cards_by_status.setdefault(bucket, []).append(card)

    return GenericKanbanBoard(columns=columns, cards_by_status=cards_by_status)


@app.get("/timeline/cards", response_model=list[CardSummary])
async def timeline_cards(
    kind: CardKind = "task",
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> list[CardSummary]:
    if kind == "task":
        raw_items = await erp.list_tasks(status=None, limit=limit)
    elif kind == "work_order":
        raw_items = await erp.list_work_orders(status=None, limit=limit)
    else:
        raw_items = await erp.list_job_cards(status=None, limit=limit)

    out: list[CardSummary] = []
    for raw in raw_items:
        card = _to_card(kind, raw)
        if card is not None:
            out.append(card)
    return out


@app.get("/orders", response_model=list[SalesOrderSummary])
async def list_orders(
    limit: int = 50,
    status: str | None = None,
    erp: ERPNextClient = Depends(get_erp),
) -> list[SalesOrderSummary]:
    raw_items = await erp.list_sales_orders(status=status, limit=limit)
    out: list[SalesOrderSummary] = []
    for raw in raw_items:
        so = _to_sales_order(raw)
        if so is not None:
            out.append(so)
    return out


@app.get("/orders/{sales_order}/tasks", response_model=OrderTasksResponse)
async def order_tasks(
    sales_order: str,
    task_kind: TaskLikeKind = "work_order",
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> OrderTasksResponse:
    current = await erp.get_sales_order(sales_order)
    data = current.get("data")
    if not isinstance(data, dict):
        raise HTTPException(status_code=404, detail="Sales Order not found")

    so = _to_sales_order(data) or SalesOrderSummary(name=sales_order)

    # In standard ERPNext, Work Order links to Sales Order (field: sales_order).
    # Task does not reliably link to Sales Order without custom fields.
    if task_kind != "work_order":
        raise HTTPException(
            status_code=400,
            detail="task_kind=task is not supported for orders in the starter version (ERPNext Task is not linked to Sales Order by default). Use task_kind=work_order.",
        )

    raw_items = await erp.list_work_orders(status=None, sales_order=sales_order, limit=limit)
    tasks: list[CardSummary] = []
    for raw in raw_items:
        card = _to_card("work_order", raw)
        if card is not None:
            tasks.append(card)

    return OrderTasksResponse(sales_order=so, task_kind=task_kind, tasks=tasks)


@app.get("/orders/{sales_order}/kanban", response_model=OrderKanbanBoard)
async def order_kanban_board(
    sales_order: str,
    task_kind: TaskLikeKind = "work_order",
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> OrderKanbanBoard:
    current = await erp.get_sales_order(sales_order)
    data = current.get("data")
    if not isinstance(data, dict):
        raise HTTPException(status_code=404, detail="Sales Order not found")

    so = _to_sales_order(data) or SalesOrderSummary(name=sales_order)

    if task_kind != "work_order":
        raise HTTPException(
            status_code=400,
            detail="task_kind=task is not supported for orders in the starter version. Use task_kind=work_order.",
        )

    workflow = _resolve_workflow("work_order")
    columns = [KanbanColumn(key=k, title=t) for (k, t) in workflow.columns]

    cards_by_status: dict[str, list[CardSummary]] = {k: [] for (k, _) in workflow.columns}
    cards_by_status.setdefault("Other", [])

    raw_items = await erp.list_work_orders(status=None, sales_order=sales_order, limit=limit)
    for raw in raw_items:
        card = _to_card("work_order", raw)
        if card is None:
            continue
        bucket = _bucket_status(workflow, card.status)
        cards_by_status.setdefault(bucket, []).append(card)

    board = GenericKanbanBoard(columns=columns, cards_by_status=cards_by_status)
    return OrderKanbanBoard(sales_order=so, task_kind=task_kind, board=board)


@app.get("/work-orders/{work_order}/job-cards", response_model=list[JobCardDetail])
async def work_order_job_cards(
    work_order: str,
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> list[JobCardDetail]:
    raw_items = await erp.list_job_cards(status=None, work_order=work_order, limit=limit)
    out: list[JobCardDetail] = []
    for raw in raw_items:
        jc = _to_job_card_detail(raw)
        if jc is not None:
            out.append(jc)
    return out


@app.get("/tasks/{task_kind}/{name}/job-cards", response_model=list[JobCardDetail])
async def task_job_cards_alias(
    task_kind: TaskLikeKind,
    name: str,
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> list[JobCardDetail]:
    if task_kind != "work_order":
        raise HTTPException(
            status_code=400,
            detail="Job Cards are linked to Work Orders in ERPNext. Use /work-orders/{work_order}/job-cards or task_kind=work_order.",
        )
    raw_items = await erp.list_job_cards(status=None, work_order=name, limit=limit)
    out: list[JobCardDetail] = []
    for raw in raw_items:
        jc = _to_job_card_detail(raw)
        if jc is not None:
            out.append(jc)
    return out


@app.post(
    "/cards/{kind}/{name}/transition",
    response_model=CardTransitionResult,
    responses={400: {"model": ErrorResponse}, 404: {"model": ErrorResponse}},
)
async def transition_card(
    kind: CardKind,
    name: str,
    req: CardTransitionRequest,
    erp: ERPNextClient = Depends(get_erp),
) -> CardTransitionResult:
    workflow = _resolve_workflow(kind)

    if kind == "task":
        current = await erp.get_task(name)
        data = current.get("data")
    elif kind == "work_order":
        current = await erp.get_work_order(name)
        data = current.get("data")
    else:
        current = await erp.get_job_card(name)
        data = current.get("data")

    if not isinstance(data, dict):
        raise HTTPException(status_code=404, detail="Card not found")

    from_status = str(data.get("status") or "")
    to_status = req.to_status
    if not from_status:
        raise HTTPException(status_code=400, detail="Card status missing")

    if not workflow.can_transition(from_status, to_status):
        raise HTTPException(status_code=400, detail=f"Transition not allowed: {from_status} -> {to_status}")

    # Apply is only supported for Task in the starter implementation.
    if not req.apply:
        return CardTransitionResult(
            ok=True,
            kind=kind,
            name=name,
            from_status=from_status,
            to_status=to_status,
            applied=False,
            message="validated (apply=false)",
        )

    if kind != "task":
        raise HTTPException(
            status_code=400,
            detail="apply=true is only supported for kind=task in the starter version (Work Order / Job Card are validate-only)",
        )

    if from_status == to_status:
        return CardTransitionResult(
            ok=True,
            kind=kind,
            name=name,
            from_status=from_status,
            to_status=to_status,
            applied=False,
            message="no-op (same status)",
            erpnext_response=None,
        )

    updated = await erp.update_task_status(name, to_status)
    return CardTransitionResult(
        ok=True,
        kind=kind,
        name=name,
        from_status=from_status,
        to_status=to_status,
        applied=True,
        message="applied", 
        erpnext_response=updated,
    )


@app.post("/webhooks/erpnext", response_model=WebhookAck, responses={401: {"model": ErrorResponse}})
async def erpnext_webhook(
    payload: dict[str, Any],
    x_webhook_token: str | None = Header(default=None),
) -> WebhookAck:
    expected = settings.webhook_token
    if expected:
        if not x_webhook_token or x_webhook_token != expected:
            raise HTTPException(status_code=401, detail="Invalid webhook token")

    # Starter: just acknowledge. Team can expand to enqueue events, update cache, etc.
    return WebhookAck(ok=True, message="received")
