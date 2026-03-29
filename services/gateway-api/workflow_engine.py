from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class WorkflowDefinition:
    columns: list[tuple[str, str]]
    allowed_transitions: dict[str, set[str]]

    def bucket(self, status: str) -> str:
        keys = {k for (k, _) in self.columns}
        return status if status in keys else "Other"

    def can_transition(self, from_status: str, to_status: str) -> bool:
        if from_status == to_status:
            return True
        return to_status in self.allowed_transitions.get(from_status, set())


# Starter workflow for ERPNext Work Order.
# Keep it tolerant: ERPNext statuses vary by version/setup.
WORK_ORDER_WORKFLOW = WorkflowDefinition(
    columns=[
        ("Draft", "Draft"),
        ("Not Started", "Not Started"),
        ("In Process", "In Process"),
        ("Completed", "Completed"),
        ("Cancelled", "Cancelled"),
        ("Other", "Other"),
    ],
    allowed_transitions={
        "Draft":       {"Not Started", "In Process", "Completed", "Cancelled"},
        "Not Started": {"In Process", "Cancelled"},
        "In Process":  {"Completed", "Cancelled"},
        "Completed":   set(),
        "Cancelled":   set(),
    },
)
