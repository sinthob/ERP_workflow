from __future__ import annotations

from datetime import datetime
from typing import Any

import httpx
from fastapi import Depends, FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from pydantic_settings import BaseSettings, SettingsConfigDict

from erpnext_client import ERPNextClient
from normalization import (
    normalize_customer_payload,
    normalize_item_payload,
    normalize_price_list_payload,
    normalize_supplier_payload,
    normalize_uom_payload,
)
from schemas import (
    BulkImportRequest,
    BulkImportResult,
    CustomerCreate,
    ErrorResponse,
    ItemCreate,
    PriceListCreate,
    SupplierCreate,
    UOMCreate,
)

from team3_models import (
    JobCardCreate,
    JobCardSummary,
    JobCardUpdate,
    KanbanBoard,
    KanbanColumn,
    SalesOrderCreate,
    SalesOrderSummary,
    SalesOrderUpdate,
    TimelineItem,
    TimelineResponse,
    TransitionRequest,
    TransitionResult,
    WorkOrderCreate,
    WorkOrderSummary,
    WorkOrderUpdate,
)
from workflow_engine import WORK_ORDER_WORKFLOW


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    erp_base_url: str = "http://localhost:8080"
    erp_api_key: str | None = None
    erp_api_secret: str | None = None


settings = Settings()

app = FastAPI(title="ERP_workflow API", version="0.1.0")


@app.exception_handler(httpx.HTTPStatusError)
async def httpx_status_error_handler(_: Request, exc: httpx.HTTPStatusError) -> JSONResponse:
    detail: Any = exc.response.text
    try:
        detail = exc.response.json()
    except Exception:
        pass

    return JSONResponse(
        status_code=exc.response.status_code,
        content={
            "detail": detail,
            "upstream": {"url": str(exc.request.url), "status_code": exc.response.status_code},
        },
    )


@app.exception_handler(httpx.RequestError)
async def httpx_request_error_handler(_: Request, exc: httpx.RequestError) -> JSONResponse:
    return JSONResponse(
        status_code=502,
        content={"detail": f"Upstream request failed: {exc.__class__.__name__}: {exc}"},
    )


def get_erp() -> ERPNextClient:
    return ERPNextClient(
        base_url=settings.erp_base_url,
        api_key=settings.erp_api_key,
        api_secret=settings.erp_api_secret,
    )


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/erp/ping")
async def erp_ping() -> dict:
    """Ping ERPNext/Frappe via the public ping method.

    Works with the demo compose stack in frappe_docker/pwd.yml.
    """

    url = settings.erp_base_url.rstrip("/") + "/api/method/ping"
    timeout = httpx.Timeout(10.0, connect=5.0)

    async with httpx.AsyncClient(timeout=timeout) as client:
        resp = await client.get(url)
        resp.raise_for_status()
        data = resp.json()

    return {"erp_base_url": settings.erp_base_url, "response": data}


async def _bulk_create(erp: ERPNextClient, doctype: str, items: list[dict[str, Any]]) -> BulkImportResult:
    results: list[dict[str, Any]] = []
    created = 0
    failed = 0

    for item in items:
        try:
            res = await erp.create_doc(doctype, item)
            results.append({"ok": True, "result": res})
            created += 1
        except httpx.HTTPStatusError as ex:
            detail: Any = ex.response.text
            try:
                detail = ex.response.json()
            except Exception:
                pass
            results.append({"ok": False, "error": detail})
            failed += 1

    return BulkImportResult(ok=failed == 0, created=created, failed=failed, results=results)


def _raise_upstream_error(ex: httpx.HTTPStatusError) -> None:
    detail: Any = ex.response.text
    try:
        detail = ex.response.json()
    except Exception:
        pass
    raise HTTPException(status_code=ex.response.status_code, detail=detail)


def _drop_none(payload: dict[str, Any]) -> dict[str, Any]:
    return {k: v for k, v in payload.items() if v is not None}


def parse_erpnext_datetime(value: Any) -> datetime | None:
    if not value:
        return None
    if isinstance(value, datetime):
        return value
    if isinstance(value, str):
        try:
            return datetime.fromisoformat(value.replace(" ", "T"))
        except ValueError:
            return None
    return None


def _to_sales_order_summary(raw: dict[str, Any]) -> SalesOrderSummary | None:
    name = raw.get("name")
    if not name:
        return None
    return SalesOrderSummary(
        name=str(name),
        title=raw.get("title"),
        customer=raw.get("customer"),
        status=raw.get("status"),
        transaction_date=raw.get("transaction_date"),
        delivery_date=raw.get("delivery_date"),
        modified=parse_erpnext_datetime(raw.get("modified")),
    )


def _to_work_order_summary(raw: dict[str, Any]) -> WorkOrderSummary | None:
    name = raw.get("name")
    if not name:
        return None
    qty = raw.get("qty")
    try:
        qty_f = float(qty) if qty is not None else None
    except (TypeError, ValueError):
        qty_f = None

    return WorkOrderSummary(
        name=str(name),
        production_item=raw.get("production_item"),
        sales_order=raw.get("sales_order"),
        status=raw.get("status"),
        qty=qty_f,
        modified=parse_erpnext_datetime(raw.get("modified")),
    )


def _to_job_card_summary(raw: dict[str, Any]) -> JobCardSummary | None:
    name = raw.get("name")
    if not name:
        return None
    for_qty = raw.get("for_quantity")
    try:
        for_qty_f = float(for_qty) if for_qty is not None else None
    except (TypeError, ValueError):
        for_qty_f = None

    return JobCardSummary(
        name=str(name),
        work_order=raw.get("work_order"),
        operation=raw.get("operation"),
        workstation=raw.get("workstation"),
        status=raw.get("status"),
        for_quantity=for_qty_f,
        modified=parse_erpnext_datetime(raw.get("modified")),
    )


# ------------------------------
# Team 3: Orders / Work Orders / Job Cards (CRUD)
# ------------------------------


@app.get("/sales-orders", response_model=list[SalesOrderSummary])
async def list_sales_orders(
    q: str | None = None,
    status: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[SalesOrderSummary]:
    filters: list[list[Any]] | None = None
    if q:
        filters = [["Sales Order", "name", "like", f"%{q}%"]]
    if status:
        extra = ["Sales Order", "status", "=", status]
        if filters is None:
            filters = [extra]
        else:
            filters.append(extra)

    rows = await erp.list_resource(
        "Sales Order",
        fields=["name", "title", "customer", "status", "transaction_date", "delivery_date", "modified"],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )

    out: list[SalesOrderSummary] = []
    for r in rows:
        so = _to_sales_order_summary(r)
        if so is not None:
            out.append(so)
    return out


@app.get("/sales-orders/{name}")
async def get_sales_order(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Sales Order", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/sales-orders", responses={400: {"model": ErrorResponse}})
async def create_sales_order(body: SalesOrderCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = body.model_dump(exclude_none=True)
    if body.items:
        payload["items"] = [i.model_dump(exclude_none=True) for i in body.items]
    payload = _drop_none(payload)

    # Convert date fields to string for ERPNext
    for date_field in ("transaction_date", "delivery_date"):
        if date_field in payload and payload[date_field] is not None:
            payload[date_field] = str(payload[date_field])

    try:
        created = await erp.create_doc("Sales Order", payload)
        # ERPNext requires SO to be Submitted before it can be linked to a Work Order
        so_name = (created.get("data") or {}).get("name") or created.get("name")
        if so_name:
            await erp.submit_doc("Sales Order", so_name)
        return created
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/sales-orders/{name}")
async def update_sales_order(name: str, body: SalesOrderUpdate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = _drop_none(body.model_dump())
    if "transaction_date" in payload and payload["transaction_date"] is not None:
        payload["transaction_date"] = str(payload["transaction_date"])
    try:
        return await erp.update_doc("Sales Order", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/sales-orders/{name}")
async def delete_sales_order(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Sales Order", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.get("/work-orders", response_model=list[WorkOrderSummary])
async def list_work_orders(
    sales_order: str | None = None,
    status: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[WorkOrderSummary]:
    filters: list[list[Any]] | None = None
    if sales_order:
        filters = [["Work Order", "sales_order", "=", sales_order]]
    if status:
        extra = ["Work Order", "status", "=", status]
        if filters is None:
            filters = [extra]
        else:
            filters.append(extra)

    rows = await erp.list_resource(
        "Work Order",
        fields=["name", "production_item", "sales_order", "status", "qty", "modified"],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )

    out: list[WorkOrderSummary] = []
    for r in rows:
        wo = _to_work_order_summary(r)
        if wo is not None:
            out.append(wo)
    return out


@app.get("/work-orders/{name}")
async def get_work_order(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Work Order", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/work-orders", responses={400: {"model": ErrorResponse}})
async def create_work_order(body: WorkOrderCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = _drop_none(body.model_dump())
    try:
        created = await erp.create_doc("Work Order", payload)
        # Submit WO immediately so it enters "Not Started" state and can be transitioned
        wo_name = (created.get("data") or {}).get("name") or created.get("name")
        if wo_name:
            await erp.submit_doc("Work Order", wo_name)
        return created
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/work-orders/{name}")
async def update_work_order(name: str, body: WorkOrderUpdate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = _drop_none(body.model_dump())
    # Guardrail: status changes must go through workflow endpoint.
    payload.pop("status", None)
    try:
        return await erp.update_doc("Work Order", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/work-orders/{name}")
async def delete_work_order(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Work Order", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.get("/job-cards", response_model=list[JobCardSummary])
async def list_job_cards(
    work_order: str | None = None,
    status: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[JobCardSummary]:
    filters: list[list[Any]] | None = None
    if work_order:
        filters = [["Job Card", "work_order", "=", work_order]]
    if status:
        extra = ["Job Card", "status", "=", status]
        if filters is None:
            filters = [extra]
        else:
            filters.append(extra)

    rows = await erp.list_resource(
        "Job Card",
        fields=[
            "name",
            "work_order",
            "operation",
            "workstation",
            "status",
            "for_quantity",
            "modified",
        ],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )

    out: list[JobCardSummary] = []
    for r in rows:
        jc = _to_job_card_summary(r)
        if jc is not None:
            out.append(jc)
    return out


@app.get("/job-cards/{name}")
async def get_job_card(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Job Card", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/job-cards", responses={400: {"model": ErrorResponse}})
async def create_job_card(body: JobCardCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = _drop_none(body.model_dump())
    if "posting_date" in payload and payload["posting_date"] is not None:
        payload["posting_date"] = str(payload["posting_date"])
    try:
        return await erp.create_doc("Job Card", payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/job-cards/{name}")
async def update_job_card(name: str, body: JobCardUpdate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = _drop_none(body.model_dump())
    if "posting_date" in payload and payload["posting_date"] is not None:
        payload["posting_date"] = str(payload["posting_date"])
    try:
        return await erp.update_doc("Job Card", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/job-cards/{name}")
async def delete_job_card(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Job Card", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


# ------------------------------
# Team 3: Workflow / Views
# ------------------------------


@app.post(
    "/workflow/work-orders/{name}/transition",
    response_model=TransitionResult,
    responses={400: {"model": ErrorResponse}, 404: {"model": ErrorResponse}},
)
async def transition_work_order(
    name: str,
    req: TransitionRequest,
    erp: ERPNextClient = Depends(get_erp),
) -> TransitionResult:
    current = await erp.get_doc("Work Order", name)
    data = current.get("data")
    if not isinstance(data, dict):
        raise HTTPException(status_code=404, detail="Work Order not found")

    from_status = str(data.get("status") or "")
    to_status = req.to_status
    if not from_status:
        raise HTTPException(status_code=400, detail="Work Order status missing")

    if not WORK_ORDER_WORKFLOW.can_transition(from_status, to_status):
        raise HTTPException(status_code=400, detail=f"Transition not allowed: {from_status} -> {to_status}")

    if from_status == to_status:
        return TransitionResult(ok=True, from_status=from_status, to_status=to_status, applied=False)

    # If the WO is still Draft (docstatus=0) it must be submitted first.
    if from_status == "Draft":
        await erp.submit_doc("Work Order", name)
        # After submit ERPNext sets status to "Not Started" automatically.
        # If target is beyond Not Started, force the status via Server Script.
        if to_status != "Not Started":
            await erp.force_wo_status(name, to_status)
    else:
        # WO already submitted — ERPNext blocks status changes through normal save().
        # Use force_wo_status which writes directly to DB via Server Script.
        await erp.force_wo_status(name, to_status)

    updated = await erp.get_doc("Work Order", name)
    return TransitionResult(ok=True, from_status=from_status, to_status=to_status, applied=True, erpnext_response=updated)


@app.get("/views/orders/{sales_order}/kanban", response_model=KanbanBoard)
async def order_kanban_view(
    sales_order: str,
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> KanbanBoard:
    columns = [KanbanColumn(key=k, title=t) for (k, t) in WORK_ORDER_WORKFLOW.columns]
    grouped: dict[str, list[WorkOrderSummary]] = {k: [] for (k, _) in WORK_ORDER_WORKFLOW.columns}
    grouped.setdefault("Other", [])

    rows = await erp.list_resource(
        "Work Order",
        fields=["name", "production_item", "sales_order", "status", "qty", "modified"],
        filters=[["Work Order", "sales_order", "=", sales_order]],
        limit_page_length=limit,
        limit_start=0,
        order_by="modified desc",
    )
    for r in rows:
        wo = _to_work_order_summary(r)
        if wo is None:
            continue
        bucket = WORK_ORDER_WORKFLOW.bucket(wo.status or "")
        grouped.setdefault(bucket, []).append(wo)

    return KanbanBoard(columns=columns, items_by_status=grouped)


@app.get("/views/orders/{sales_order}/timeline", response_model=TimelineResponse)
async def order_timeline_view(
    sales_order: str,
    limit: int = 100,
    erp: ERPNextClient = Depends(get_erp),
) -> TimelineResponse:
    rows = await erp.list_resource(
        "Work Order",
        fields=["name", "production_item", "sales_order", "status", "modified"],
        filters=[["Work Order", "sales_order", "=", sales_order]],
        limit_page_length=limit,
        limit_start=0,
        order_by="modified desc",
    )

    items: list[TimelineItem] = []
    for r in rows:
        name = r.get("name")
        if not name:
            continue
        items.append(
            TimelineItem(
                name=str(name),
                title=r.get("production_item"),
                status=r.get("status"),
                modified=parse_erpnext_datetime(r.get("modified")),
            )
        )

    return TimelineResponse(sales_order=sales_order, items=items)


@app.get("/views/work-orders/{work_order}/job-cards", response_model=list[JobCardSummary])
async def work_order_job_cards_view(
    work_order: str,
    limit: int = 100,
    erp: ERPNextClient = Depends(get_erp),
) -> list[JobCardSummary]:
    rows = await erp.list_resource(
        "Job Card",
        fields=["name", "work_order", "operation", "workstation", "status", "for_quantity", "modified"],
        filters=[["Job Card", "work_order", "=", work_order]],
        limit_page_length=limit,
        limit_start=0,
        order_by="modified desc",
    )
    out: list[JobCardSummary] = []
    for r in rows:
        jc = _to_job_card_summary(r)
        if jc is not None:
            out.append(jc)
    return out


# ------------------------------
# Master Data: Customer
# ------------------------------


@app.get("/master/customers")
async def list_customers(
    q: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    filters = None
    if q:
        filters = [["Customer", "customer_name", "like", f"%{q}%"]]
    return await erp.list_resource(
        "Customer",
        fields=["name", "customer_name", "customer_type", "mobile_no", "email_id", "modified"],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )


@app.get("/master/customers/{name}")
async def get_customer(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Customer", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/customers", responses={400: {"model": ErrorResponse}})
async def create_customer(body: CustomerCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_customer_payload(body.model_dump(exclude_none=True))
    try:
        return await erp.create_doc("Customer", payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/master/customers/{name}")
async def update_customer(name: str, body: dict[str, Any], erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_customer_payload(body)
    try:
        return await erp.update_doc("Customer", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/master/customers/{name}")
async def delete_customer(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Customer", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/customers/bulk-import", response_model=BulkImportResult)
async def bulk_import_customers(req: BulkImportRequest, erp: ERPNextClient = Depends(get_erp)) -> BulkImportResult:
    normalized = [normalize_customer_payload(x) for x in req.items]
    return await _bulk_create(erp, "Customer", normalized)


# ------------------------------
# Master Data: Supplier
# ------------------------------


@app.get("/master/suppliers")
async def list_suppliers(
    q: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    filters = None
    if q:
        filters = [["Supplier", "supplier_name", "like", f"%{q}%"]]
    return await erp.list_resource(
        "Supplier",
        fields=["name", "supplier_name", "supplier_type", "supplier_group", "modified"],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )


@app.get("/master/suppliers/{name}")
async def get_supplier(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Supplier", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/suppliers", responses={400: {"model": ErrorResponse}})
async def create_supplier(body: SupplierCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_supplier_payload(body.model_dump(exclude_none=True))
    try:
        return await erp.create_doc("Supplier", payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/master/suppliers/{name}")
async def update_supplier(name: str, body: dict[str, Any], erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_supplier_payload(body)
    try:
        return await erp.update_doc("Supplier", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/master/suppliers/{name}")
async def delete_supplier(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Supplier", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/suppliers/bulk-import", response_model=BulkImportResult)
async def bulk_import_suppliers(req: BulkImportRequest, erp: ERPNextClient = Depends(get_erp)) -> BulkImportResult:
    normalized = [normalize_supplier_payload(x) for x in req.items]
    return await _bulk_create(erp, "Supplier", normalized)


# ------------------------------
# Master Data: UOM
# ------------------------------


@app.get("/master/uoms")
async def list_uoms(limit: int = 50, offset: int = 0, erp: ERPNextClient = Depends(get_erp)) -> list[dict[str, Any]]:
    return await erp.list_resource(
        "UOM",
        fields=["name", "uom_name", "modified"],
        filters=None,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )


@app.get("/master/uoms/{name}")
async def get_uom(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("UOM", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/uoms", responses={400: {"model": ErrorResponse}})
async def create_uom(body: UOMCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_uom_payload(body.model_dump(exclude_none=True))
    try:
        return await erp.create_doc("UOM", payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/master/uoms/{name}")
async def update_uom(name: str, body: dict[str, Any], erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_uom_payload(body)
    try:
        return await erp.update_doc("UOM", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/master/uoms/{name}")
async def delete_uom(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("UOM", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/uoms/bulk-import", response_model=BulkImportResult)
async def bulk_import_uoms(req: BulkImportRequest, erp: ERPNextClient = Depends(get_erp)) -> BulkImportResult:
    normalized = [normalize_uom_payload(x) for x in req.items]
    return await _bulk_create(erp, "UOM", normalized)


# ------------------------------
# Master Data: Item
# ------------------------------


@app.get("/master/items")
async def list_items(
    q: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    filters = None
    if q:
        filters = [["Item", "item_name", "like", f"%{q}%"]]
    return await erp.list_resource(
        "Item",
        fields=["name", "item_code", "item_name", "item_group", "stock_uom", "is_stock_item", "modified"],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )


@app.get("/master/items/{name}")
async def get_item(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Item", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/items", responses={400: {"model": ErrorResponse}})
async def create_item(body: ItemCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_item_payload(body.model_dump(exclude_none=True))
    try:
        return await erp.create_doc("Item", payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/master/items/{name}")
async def update_item(name: str, body: dict[str, Any], erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_item_payload(body)
    try:
        return await erp.update_doc("Item", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/master/items/{name}")
async def delete_item(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Item", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/items/bulk-import", response_model=BulkImportResult)
async def bulk_import_items(req: BulkImportRequest, erp: ERPNextClient = Depends(get_erp)) -> BulkImportResult:
    normalized = [normalize_item_payload(x) for x in req.items]
    return await _bulk_create(erp, "Item", normalized)


# ------------------------------
# Master Data: Price List
# ------------------------------


@app.get("/master/price-lists")
async def list_price_lists(
    q: str | None = None,
    limit: int = 20,
    offset: int = 0,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    filters = None
    if q:
        filters = [["Price List", "price_list_name", "like", f"%{q}%"]]
    return await erp.list_resource(
        "Price List",
        fields=["name", "price_list_name", "currency", "buying", "selling", "modified"],
        filters=filters,
        limit_page_length=limit,
        limit_start=offset,
        order_by="modified desc",
    )


@app.get("/master/price-lists/{name}")
async def get_price_list(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.get_doc("Price List", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/price-lists", responses={400: {"model": ErrorResponse}})
async def create_price_list(body: PriceListCreate, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    payload = normalize_price_list_payload(body.model_dump(exclude_none=True))
    try:
        return await erp.create_doc("Price List", payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.put("/master/price-lists/{name}")
async def update_price_list(
    name: str,
    body: dict[str, Any],
    erp: ERPNextClient = Depends(get_erp),
) -> dict[str, Any]:
    payload = normalize_price_list_payload(body)
    try:
        return await erp.update_doc("Price List", name, payload)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.delete("/master/price-lists/{name}")
async def delete_price_list(name: str, erp: ERPNextClient = Depends(get_erp)) -> dict[str, Any]:
    try:
        return await erp.delete_doc("Price List", name)
    except httpx.HTTPStatusError as ex:
        _raise_upstream_error(ex)


@app.post("/master/price-lists/bulk-import", response_model=BulkImportResult)
async def bulk_import_price_lists(req: BulkImportRequest, erp: ERPNextClient = Depends(get_erp)) -> BulkImportResult:
    normalized = [normalize_price_list_payload(x) for x in req.items]
    return await _bulk_create(erp, "Price List", normalized)


@app.get("/master/warehouses")
async def list_warehouses(
    limit: int = 100,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    return await erp.list_resource(
        "Warehouse",
        fields=["name", "warehouse_name", "warehouse_type", "is_group"],
        filters=[["Warehouse", "is_group", "=", 0]],
        limit_page_length=limit,
        order_by="name asc",
    )


@app.get("/master/operations")
async def list_operations(
    limit: int = 100,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    return await erp.list_resource(
        "Operation",
        fields=["name", "description"],
        limit_page_length=limit,
        order_by="name asc",
    )


@app.get("/master/workstations")
async def list_workstations(
    limit: int = 100,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    return await erp.list_resource(
        "Workstation",
        fields=["name", "workstation_type", "warehouse"],
        limit_page_length=limit,
        order_by="name asc",
    )


# ------------------------------
# Master Data: Company
# ------------------------------


@app.get("/master/companies")
async def list_companies(
    limit: int = 50,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    return await erp.list_resource(
        "Company",
        fields=["name", "company_name", "abbr"],
        limit_page_length=limit,
        order_by="name asc",
    )


# ------------------------------
# Master Data: BOM (Bill of Materials)
# ------------------------------


@app.get("/master/boms")
async def list_boms(
    item: str | None = None,
    limit: int = 100,
    erp: ERPNextClient = Depends(get_erp),
) -> list[dict[str, Any]]:
    # Return all BOMs (draft=0 and submitted=1) so user can see what exists
    filters: list[Any] = []
    if item:
        filters.append(["BOM", "item", "=", item])
    rows = await erp.list_resource(
        "BOM",
        fields=["name", "item", "item_name", "is_default", "docstatus", "modified"],
        filters=filters if filters else None,
        limit_page_length=limit,
        order_by="modified desc",
    )
    # Label draft BOMs so UI can warn the user
    for r in rows:
        if r.get("docstatus", 1) != 1:
            r["_draft"] = True
    return rows
