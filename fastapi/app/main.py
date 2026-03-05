from __future__ import annotations

import httpx
from fastapi import FastAPI

from .settings import settings

app = FastAPI(title="ERP_workflow API", version="0.1.0")


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.get("/erp/ping")
async def erp_ping() -> dict:
    """Ping ERPNext/Frappe via the public ping method.

    Works with the demo compose stack in frappe_docker/pwd.yml.
    """

    url = settings.erp_base_url.rstrip("/") + "/api/method/ping"
    timeout = httpx.Timeout(10.0, connect=5.0)

    async with httpx.AsyncClient(timeout=timeout) as client:
        resp = await client.get(url)
        resp.raise_for_status()
        data = resp.json()

    return {"erp_base_url": settings.erp_base_url, "response": data}
