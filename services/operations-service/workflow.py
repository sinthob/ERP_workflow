from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class WorkflowDefinition:
    columns: list[tuple[str, str]]
    allowed_transitions: dict[str, set[str]]

    def can_transition(self, from_status: str, to_status: str) -> bool:
        if from_status == to_status:
            return True
        return to_status in self.allowed_transitions.get(from_status, set())


# Minimal starter workflow for ERPNext Task
# ERPNext Task status commonly: Open, Working, Pending Review, Completed, Cancelled
TASK_WORKFLOW = WorkflowDefinition(
    columns=[
        ("Open", "Open"),
        ("Working", "Working"),
        ("Pending Review", "Pending Review"),
        ("Completed", "Completed"),
        ("Other", "Other"),
    ],
    allowed_transitions={
        "Open": {"Working", "Cancelled"},
        "Working": {"Pending Review", "Completed", "Cancelled"},
        "Pending Review": {"Working", "Completed", "Cancelled"},
        "Completed": set(),
        "Cancelled": set(),
    },
)


# Simple starter workflow for ERPNext Work Order (generic)
# ERPNext Work Order statuses can vary by version/setup; keep this minimal and tolerant.
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
        "Draft": {"Not Started", "Cancelled"},
        "Not Started": {"In Process", "Cancelled"},
        "In Process": {"Completed", "Cancelled"},
        "Completed": set(),
        "Cancelled": set(),
    },
)


# Simple starter workflow for ERPNext Job Card (generic)
# ERPNext Job Card may have system-driven transitions; keep validate-only at first.
JOB_CARD_WORKFLOW = WorkflowDefinition(
    columns=[
        ("Open", "Open"),
        ("Work In Progress", "Work In Progress"),
        ("On Hold", "On Hold"),
        ("Completed", "Completed"),
        ("Cancelled", "Cancelled"),
        ("Other", "Other"),
    ],
    allowed_transitions={
        "Open": {"Work In Progress", "On Hold", "Cancelled"},
        "Work In Progress": {"On Hold", "Completed", "Cancelled"},
        "On Hold": {"Work In Progress", "Cancelled"},
        "Completed": set(),
        "Cancelled": set(),
    },
)
