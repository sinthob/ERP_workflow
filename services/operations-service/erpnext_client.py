from __future__ import annotations

import json
from datetime import datetime
from typing import Any

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
                self._url(f"/api/resource/{doctype}"),
                params=params,
                headers=self._auth_header,
            )
            resp.raise_for_status()
            payload = resp.json()

        data = payload.get("data")
        if not isinstance(data, list):
            return []
        return data

    async def get_task(self, name: str) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.get(
                self._url(f"/api/resource/Task/{name}"),
                headers=self._auth_header,
            )
            resp.raise_for_status()
            return resp.json()

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
        payload = {"status": to_status}
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.put(
                self._url(f"/api/resource/Task/{name}"),
                json=payload,
                headers=self._auth_header,
            )
            resp.raise_for_status()
            return resp.json()


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
