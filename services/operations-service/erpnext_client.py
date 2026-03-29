from __future__ import annotations

import json
from datetime import datetime
from typing import Any
from urllib.parse import quote

import httpx


class ERPNextClient:
    def __init__(
        self,
        *,
        base_url: str,
        api_key: str | None = None,
        api_secret: str | None = None,
        timeout: httpx.Timeout | None = None,
    ) -> None:
        self._base_url = base_url.rstrip("/")
        self._auth_header = None
        if api_key and api_secret:
            self._auth_header = {"Authorization": f"token {api_key}:{api_secret}"}
        self._timeout = timeout or httpx.Timeout(20.0, connect=5.0)

    def _url(self, path: str) -> str:
        return self._base_url + path

    @staticmethod
    def _seg(value: str) -> str:
        return quote(value, safe="")

    def _resource_url(self, doctype: str, name: str | None = None) -> str:
        if name is None:
            return self._url(f"/api/resource/{self._seg(doctype)}")
        return self._url(f"/api/resource/{self._seg(doctype)}/{self._seg(name)}")

    async def ping(self) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.get(self._url("/api/method/ping"), headers=self._auth_header)
            resp.raise_for_status()
            return resp.json()

    async def list_resource(
        self,
        doctype: str,
        *,
        fields: list[str],
        filters: list[list[Any]] | None = None,
        limit_page_length: int = 50,
        order_by: str | None = None,
    ) -> list[dict[str, Any]]:
        params: dict[str, Any] = {
            "fields": json.dumps(fields),
            "limit_page_length": str(limit_page_length),
        }
        if filters is not None:
            params["filters"] = json.dumps(filters)
        if order_by:
            params["order_by"] = order_by

        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.get(
                self._resource_url(doctype),
                params=params,
                headers=self._auth_header,
            )
            resp.raise_for_status()
            payload = resp.json()

        data = payload.get("data")
        if not isinstance(data, list):
            return []
        return data

    async def get_doc(self, doctype: str, name: str) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.get(self._resource_url(doctype, name), headers=self._auth_header)
            resp.raise_for_status()
            return resp.json()

    async def update_doc(self, doctype: str, name: str, payload: dict[str, Any]) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.put(self._resource_url(doctype, name), json=payload, headers=self._auth_header)
            resp.raise_for_status()
            return resp.json()

    async def get_task(self, name: str) -> dict[str, Any]:
        return await self.get_doc("Task", name)

    async def list_tasks(self, *, status: str | None = None, limit: int = 50) -> list[dict[str, Any]]:
        filters: list[list[Any]] | None = None
        if status:
            filters = [["Task", "status", "=", status]]

        return await self.list_resource(
            "Task",
            fields=["name", "subject", "status", "modified"],
            filters=filters,
            limit_page_length=limit,
            order_by="modified desc",
        )

    async def update_task_status(self, name: str, to_status: str) -> dict[str, Any]:
        return await self.update_doc("Task", name, {"status": to_status})

    async def get_work_order(self, name: str) -> dict[str, Any]:
        return await self.get_doc("Work Order", name)

    async def list_job_cards(
        self,
        *,
        status: str | None = None,
        work_order: str | None = None,
        limit: int = 50,
    ) -> list[dict[str, Any]]:
        filters: list[list[Any]] | None = None

        if status:
            filters = [["Job Card", "status", "=", status]]

        if work_order:
            extra = ["Job Card", "work_order", "=", work_order]
            if filters is None:
                filters = [extra]
            else:
                filters.append(extra)

        return await self.list_resource(
            "Job Card",
            fields=["name", "work_order", "operation", "status", "for_quantity", "modified"],
            filters=filters,
            limit_page_length=limit,
            order_by="modified desc",
        )

    async def get_job_card(self, name: str) -> dict[str, Any]:
        return await self.get_doc("Job Card", name)

    async def list_sales_orders(self, *, status: str | None = None, limit: int = 50) -> list[dict[str, Any]]:
        filters: list[list[Any]] | None = None
        if status:
            filters = [["Sales Order", "status", "=", status]]

        # Keep fields minimal and commonly present.
        return await self.list_resource(
            "Sales Order",
            fields=["name", "customer", "status", "transaction_date", "modified"],
            filters=filters,
            limit_page_length=limit,
            order_by="modified desc",
        )

    async def get_sales_order(self, name: str) -> dict[str, Any]:
        return await self.get_doc("Sales Order", name)

    async def list_work_orders(
        self,
        *,
        status: str | None = None,
        sales_order: str | None = None,
        limit: int = 50,
    ) -> list[dict[str, Any]]:
        filters: list[list[Any]] | None = None

        if status:
            filters = [["Work Order", "status", "=", status]]

        if sales_order:
            extra = ["Work Order", "sales_order", "=", sales_order]
            if filters is None:
                filters = [extra]
            else:
                filters.append(extra)

        return await self.list_resource(
            "Work Order",
            fields=["name", "production_item", "sales_order", "status", "qty", "modified"],
            filters=filters,
            limit_page_length=limit,
            order_by="modified desc",
        )


def parse_erpnext_datetime(value: Any) -> datetime | None:
    if not value:
        return None
    if isinstance(value, datetime):
        return value
    if isinstance(value, str):
        # ERPNext returns e.g. "2026-03-05 10:20:30.123456" or without microseconds
        try:
            return datetime.fromisoformat(value.replace(" ", "T"))
        except ValueError:
            return None
    return None
