# FastAPI (integration layer)

Service นี้เป็นตัวกลางให้ Avalonia (Desktop) เรียกใช้งานผ่าน HTTP แล้วค่อยไปคุยกับ ERPNext/Frappe ต่อ

## Run (Windows)

```powershell
cd .\services\gateway-api
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt

# copy config
copy .env.example .env

uvicorn main:app --reload --port 8000
```

## Endpoints

- `GET /health` → health check
- `GET /erp/ping` → ลอง ping ไปที่ ERPNext (`ERP_BASE_URL`)

### Master data (starter)

Thin proxy ไปที่ ERPNext REST (`/api/resource/...`) สำหรับ master data หลัก

- Customers
	- `GET /master/customers`
	- `GET /master/customers/{name}`
	- `POST /master/customers`
	- `PUT /master/customers/{name}`
	- `DELETE /master/customers/{name}`
	- `POST /master/customers/bulk-import`
- Suppliers
	- `GET /master/suppliers`
	- `GET /master/suppliers/{name}`
	- `POST /master/suppliers`
	- `PUT /master/suppliers/{name}`
	- `DELETE /master/suppliers/{name}`
	- `POST /master/suppliers/bulk-import`
- Items
	- `GET /master/items`
	- `GET /master/items/{name}`
	- `POST /master/items`
	- `PUT /master/items/{name}`
	- `DELETE /master/items/{name}`
	- `POST /master/items/bulk-import`
- Units of Measure (UOM)
	- `GET /master/uoms`
	- `GET /master/uoms/{name}`
	- `POST /master/uoms`
	- `PUT /master/uoms/{name}`
	- `DELETE /master/uoms/{name}`
	- `POST /master/uoms/bulk-import`
- Price Lists
	- `GET /master/price-lists`
	- `GET /master/price-lists/{name}`
	- `POST /master/price-lists`
	- `PUT /master/price-lists/{name}`
	- `DELETE /master/price-lists/{name}`
	- `POST /master/price-lists/bulk-import`

## Config

อ่านจาก `.env` (ไฟล์นี้ถูก ignore):

- `ERP_BASE_URL` (default: `http://localhost:8080`)
- `ERP_BASE_URL` (default: `http://localhost:8081`)

Optional (แนะนำ):

- `ERP_API_KEY`
- `ERP_API_SECRET`

ถ้าจะให้ FastAPI ไปเรียก API ที่ต้อง auth ของ ERPNext ค่อยเพิ่ม `ERP_API_KEY` / `ERP_API_SECRET` หรือ flow แบบ login cookie ตามที่ทีมเลือก
