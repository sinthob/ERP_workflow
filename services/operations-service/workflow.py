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
    ],
    allowed_transitions={
        "Open": {"Working", "Cancelled"},
        "Working": {"Pending Review", "Completed", "Cancelled"},
        "Pending Review": {"Working", "Completed", "Cancelled"},
        "Completed": set(),
        "Cancelled": set(),
    },
)
