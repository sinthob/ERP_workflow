from __future__ import annotations

from typing import Any


def _clean_str(value: Any) -> str | None:
    if value is None:
        return None
    if not isinstance(value, str):
        value = str(value)
    value = value.strip()
    return value or None


def normalize_customer_payload(payload: dict[str, Any]) -> dict[str, Any]:
    out = dict(payload)
    if "customer_name" in out:
        out["customer_name"] = _clean_str(out.get("customer_name"))
    if "mobile_no" in out:
        out["mobile_no"] = _clean_str(out.get("mobile_no"))
    if "email_id" in out:
        out["email_id"] = _clean_str(out.get("email_id"))
    return {k: v for k, v in out.items() if v is not None}


def normalize_supplier_payload(payload: dict[str, Any]) -> dict[str, Any]:
    out = dict(payload)
    if "supplier_name" in out:
        out["supplier_name"] = _clean_str(out.get("supplier_name"))
    return {k: v for k, v in out.items() if v is not None}


def normalize_uom_payload(payload: dict[str, Any]) -> dict[str, Any]:
    out = dict(payload)
    if "uom_name" in out:
        out["uom_name"] = _clean_str(out.get("uom_name"))
    return {k: v for k, v in out.items() if v is not None}


def normalize_item_payload(payload: dict[str, Any]) -> dict[str, Any]:
    out = dict(payload)
    if "item_code" in out:
        code = _clean_str(out.get("item_code"))
        out["item_code"] = code.upper() if code else None
    if "item_name" in out:
        out["item_name"] = _clean_str(out.get("item_name"))
    if "stock_uom" in out:
        out["stock_uom"] = _clean_str(out.get("stock_uom"))
    return {k: v for k, v in out.items() if v is not None}


def normalize_price_list_payload(payload: dict[str, Any]) -> dict[str, Any]:
    out = dict(payload)
    if "price_list_name" in out:
        out["price_list_name"] = _clean_str(out.get("price_list_name"))
    if "currency" in out:
        out["currency"] = _clean_str(out.get("currency"))
    return {k: v for k, v in out.items() if v is not None}
