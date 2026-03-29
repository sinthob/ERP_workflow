from __future__ import annotations

from datetime import date, datetime
from typing import Any, Literal

from pydantic import BaseModel, Field


class SalesOrderItemCreate(BaseModel):
    item_code: str = Field(..., min_length=1)
    qty: float = Field(..., gt=0)

    # Optional, depends on ERPNext config/price rules
    rate: float | None = None


class SalesOrderCreate(BaseModel):
    customer: str = Field(..., min_length=1)
    title: str | None = None  # Human-readable SO name; defaults to customer name in ERPNext

    # If omitted, ERPNext may default; keep optional for flexibility.
    transaction_date: date | None = None
    delivery_date: date | None = None

    # For stable demo, require at least one item.
    items: list[SalesOrderItemCreate] = Field(default_factory=list)

    # Optional linkages
    company: str | None = None


class SalesOrderUpdate(BaseModel):
    # Keep updates flexible but safe; Sales Order has many fields in practice.
    customer: str | None = None
    transaction_date: date | None = None
    company: str | None = None


class SalesOrderSummary(BaseModel):
    name: str
    title: str | None = None
    customer: str | None = None
    status: str | None = None
    transaction_date: str | None = None
    delivery_date: str | None = None
    modified: datetime | None = None


class WorkOrderCreate(BaseModel):
    company: str = Field(..., min_length=1)
    production_item: str = Field(..., min_length=1)
    qty: float = Field(..., gt=0)
    bom_no: str = Field(..., min_length=1)
    fg_warehouse: str = Field(..., min_length=1)   # Target / Finished Goods Warehouse
    wip_warehouse: str = Field(..., min_length=1)  # Work-in-Progress Warehouse

    sales_order: str | None = None
    project: str | None = None

    # Optional, ERPNext can default based on DocType settings
    naming_series: str | None = None


class WorkOrderUpdate(BaseModel):
    # Keep status changes out; enforce using transition endpoint.
    production_item: str | None = None
    qty: float | None = Field(default=None, gt=0)
    bom_no: str | None = None
    sales_order: str | None = None
    project: str | None = None


class WorkOrderSummary(BaseModel):
    name: str
    production_item: str | None = None
    sales_order: str | None = None
    status: str | None = None
    qty: float | None = None
    modified: datetime | None = None


class JobCardCreate(BaseModel):
    company: str = Field(..., min_length=1)
    work_order: str = Field(..., min_length=1)

    operation: str = Field(..., min_length=1)
    workstation: str = Field(..., min_length=1)
    wip_warehouse: str = Field(..., min_length=1)

    # Optional fields
    posting_date: date | None = None
    employee: str | None = None
    for_quantity: float | None = Field(default=None, gt=0)
    naming_series: str | None = None


class JobCardUpdate(BaseModel):
    # Update only fields that are unlikely to break manufacturing logic.
    posting_date: date | None = None
    employee: str | None = None
    for_quantity: float | None = Field(default=None, gt=0)


class JobCardSummary(BaseModel):
    name: str
    work_order: str | None = None
    operation: str | None = None
    workstation: str | None = None
    status: str | None = None
    for_quantity: float | None = None
    modified: datetime | None = None


class TransitionRequest(BaseModel):
    to_status: str = Field(..., min_length=1)
    reason: str | None = None


class TransitionResult(BaseModel):
    ok: bool
    from_status: str
    to_status: str
    applied: bool
    erpnext_response: dict[str, Any] | None = None


class KanbanColumn(BaseModel):
    key: str
    title: str


class KanbanBoard(BaseModel):
    columns: list[KanbanColumn]
    items_by_status: dict[str, list[WorkOrderSummary]]


class TimelineItem(BaseModel):
    name: str
    title: str | None = None
    status: str | None = None
    modified: datetime | None = None


class TimelineResponse(BaseModel):
    sales_order: str
    items: list[TimelineItem]


TaskLikeKind = Literal["work_order"]
