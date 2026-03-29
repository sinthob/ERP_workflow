from __future__ import annotations

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
