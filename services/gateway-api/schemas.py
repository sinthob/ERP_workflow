from __future__ import annotations

from typing import Any

from pydantic import BaseModel, Field


class ErrorResponse(BaseModel):
    detail: str


class BulkImportRequest(BaseModel):
    items: list[dict[str, Any]] = Field(default_factory=list)


class BulkImportResult(BaseModel):
    ok: bool
    created: int
    failed: int
    results: list[dict[str, Any]]


class CustomerCreate(BaseModel):
    customer_name: str = Field(..., min_length=1)
    customer_type: str | None = None
    customer_group: str | None = None
    territory: str | None = None
    mobile_no: str | None = None
    email_id: str | None = None


class SupplierCreate(BaseModel):
    supplier_name: str = Field(..., min_length=1)
    supplier_type: str | None = None
    supplier_group: str | None = None


class UOMCreate(BaseModel):
    uom_name: str = Field(..., min_length=1)


class ItemCreate(BaseModel):
    item_code: str = Field(..., min_length=1)
    item_name: str = Field(..., min_length=1)
    item_group: str = Field(..., min_length=1)
    stock_uom: str | None = None
    is_stock_item: int | None = None


class PriceListCreate(BaseModel):
    price_list_name: str = Field(..., min_length=1)
    currency: str | None = None
    buying: int | None = None
    selling: int | None = None
