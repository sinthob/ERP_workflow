from __future__ import annotations

from typing import Any

from fastapi import Depends, FastAPI, Header, HTTPException
from pydantic_settings import BaseSettings, SettingsConfigDict

from erpnext_client import ERPNextClient, parse_erpnext_datetime
from models import (
    ErrorResponse,
    HealthStatus,
    KanbanBoard,
    KanbanColumn,
    TaskSummary,
    TransitionRequest,
    TransitionResult,
    WebhookAck,
)
from workflow import TASK_WORKFLOW


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    erp_base_url: str = "http://localhost:8080"
    erp_api_key: str | None = None
    erp_api_secret: str | None = None

    webhook_token: str | None = None

    service_port: int = 8003


settings = Settings()

app = FastAPI(title="operations-service", version="0.1.0")


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
