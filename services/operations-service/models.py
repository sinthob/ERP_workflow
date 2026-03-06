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


class TransitionRequest(BaseModel):
    to_status: str = Field(..., min_length=1)
    reason: str | None = None


class TransitionResult(BaseModel):
    ok: bool
    from_status: str
    to_status: str
    erpnext_response: dict[str, Any] | None = None


class WebhookAck(BaseModel):
    ok: bool
    message: str


class ErrorResponse(BaseModel):
    detail: str


HealthStatus = Literal["ok"]
