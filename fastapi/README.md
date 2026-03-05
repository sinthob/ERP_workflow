# FastAPI (integration layer)

Service นี้เป็นตัวกลางให้ Avalonia (Desktop) เรียกใช้งานผ่าน HTTP แล้วค่อยไปคุยกับ ERPNext/Frappe ต่อ

## Run (Windows)

```powershell
cd .\fastapi
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt

# copy config
copy .env.example .env

uvicorn app.main:app --reload --port 8000
```

## Endpoints

- `GET /health` → health check
- `GET /erp/ping` → ลอง ping ไปที่ ERPNext (`ERP_BASE_URL`)

## Config

อ่านจาก `.env` (ไฟล์นี้ถูก ignore):

- `ERP_BASE_URL` (default: `http://localhost:8080`)

ถ้าจะให้ FastAPI ไปเรียก API ที่ต้อง auth ของ ERPNext ค่อยเพิ่ม `ERP_API_KEY` / `ERP_API_SECRET` หรือ flow แบบ login cookie ตามที่ทีมเลือก
