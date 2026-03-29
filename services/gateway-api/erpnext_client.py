from __future__ import annotations

import json
from typing import Any
from urllib.parse import quote

import httpx

# Server Script name used to bypass Work Order status controller validation.
# Created lazily on first transition call.
_GW_SCRIPT_NAME = "gw_force_wo_status"
_GW_SCRIPT_CODE = """\
frappe.db.set_value(
    "Work Order",
    frappe.form_dict.get("wo_name"),
    "status",
    frappe.form_dict.get("status"),
)
frappe.response["message"] = {
    "name": frappe.form_dict.get("wo_name"),
    "status": frappe.form_dict.get("status"),
}
"""


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
        self._timeout = timeout or httpx.Timeout(20.0, connect=5.0)

        self._auth_header: dict[str, str] | None = None
        if api_key and api_secret:
            self._auth_header = {"Authorization": f"token {api_key}:{api_secret}"}

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
        limit_start: int = 0,
        order_by: str | None = None,
    ) -> list[dict[str, Any]]:
        params: dict[str, Any] = {
            "fields": json.dumps(fields),
            "limit_page_length": str(limit_page_length),
            "limit_start": str(limit_start),
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

    async def create_doc(self, doctype: str, data: dict[str, Any]) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.post(self._resource_url(doctype), json=data, headers=self._auth_header)
            resp.raise_for_status()
            return resp.json()

    async def update_doc(self, doctype: str, name: str, data: dict[str, Any]) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.put(self._resource_url(doctype, name), json=data, headers=self._auth_header)
            resp.raise_for_status()
            return resp.json()

    async def delete_doc(self, doctype: str, name: str) -> dict[str, Any]:
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.delete(self._resource_url(doctype, name), headers=self._auth_header)
            resp.raise_for_status()
            return resp.json()

    async def submit_doc(self, doctype: str, name: str) -> dict[str, Any]:
        """Submit a document (docstatus 0 → 1) via PUT."""
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.put(
                self._resource_url(doctype, name),
                json={"docstatus": 1},
                headers=self._auth_header,
            )
            resp.raise_for_status()
            return resp.json()

    async def _ensure_force_status_script(self) -> None:
        """Create the Server Script in ERPNext if it doesn't exist yet (idempotent)."""
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            chk = await client.get(
                self._resource_url("Server Script", _GW_SCRIPT_NAME),
                headers=self._auth_header,
            )
        if chk.status_code == 200:
            return  # already exists
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            crt = await client.post(
                self._resource_url("Server Script"),
                json={
                    "name": _GW_SCRIPT_NAME,
                    "script_type": "API",
                    "api_method": _GW_SCRIPT_NAME,
                    "allow_guest": 0,
                    "enabled": 1,
                    "script": _GW_SCRIPT_CODE,
                },
                headers=self._auth_header,
            )
            crt.raise_for_status()

    async def force_wo_status(self, name: str, status: str) -> dict[str, Any]:
        """Update Work Order status via Server Script API, bypassing ERPNext controller."""
        await self._ensure_force_status_script()
        async with httpx.AsyncClient(timeout=self._timeout) as client:
            resp = await client.post(
                self._url(f"/api/method/{_GW_SCRIPT_NAME}"),
                data={"wo_name": name, "status": status},
                headers=self._auth_header,
            )
            resp.raise_for_status()
            return resp.json()
