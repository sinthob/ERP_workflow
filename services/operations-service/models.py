from __future__ import annotations

from datetime import datetime
from typing import Any, Literal

from pydantic import BaseModel, Field


class TaskSummary(BaseModel):
    name: str
    subject: str | None = None
    status: str
    modified: datetime | None = None


class KanbanColumn(BaseModel):
    key: str
    title: str


class KanbanBoard(BaseModel):
    columns: list[KanbanColumn]
    tasks_by_status: dict[str, list[TaskSummary]]


CardKind = Literal["task", "work_order", "job_card"]


class CardSummary(BaseModel):
    kind: CardKind
    name: str
    title: str | None = None
    status: str
    modified: datetime | None = None

    # Optional references (best-effort; depends on DocType)
    sales_order: str | None = None
    work_order: str | None = None
    operation: str | None = None


class GenericKanbanBoard(BaseModel):
    columns: list[KanbanColumn]
    cards_by_status: dict[str, list[CardSummary]]


TaskLikeKind = Literal["task", "work_order"]


class SalesOrderSummary(BaseModel):
    name: str
    customer: str | None = None
    status: str | None = None
    transaction_date: str | None = None
    modified: datetime | None = None


class OrderTasksResponse(BaseModel):
    sales_order: SalesOrderSummary
    task_kind: TaskLikeKind
    tasks: list[CardSummary]


class OrderKanbanBoard(BaseModel):
    sales_order: SalesOrderSummary
    task_kind: TaskLikeKind
    board: GenericKanbanBoard


class JobCardDetail(BaseModel):
    name: str
    work_order: str | None = None
    operation: str | None = None
    for_quantity: float | None = None
    modified: datetime | None = None

    # Keep status optional; in our project scope it's treated as detail.
    status: str | None = None


class TransitionRequest(BaseModel):
    to_status: str = Field(..., min_length=1)
    reason: str | None = None


class CardTransitionRequest(BaseModel):
    to_status: str = Field(..., min_length=1)
    # For now: only Task supports apply=True. Work Order / Job Card are validate-only.
    apply: bool = False
    reason: str | None = None


class TransitionResult(BaseModel):
    ok: bool
    from_status: str
    to_status: str
    erpnext_response: dict[str, Any] | None = None


class CardTransitionResult(BaseModel):
    ok: bool
    kind: CardKind
    name: str
    from_status: str
    to_status: str
    applied: bool
    message: str | None = None
    erpnext_response: dict[str, Any] | None = None


class WebhookAck(BaseModel):
    ok: bool
    message: str


class ErrorResponse(BaseModel):
    detail: str


HealthStatus = Literal["ok"]
