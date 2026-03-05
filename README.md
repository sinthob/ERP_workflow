# ERP_workflow (local workspace)

Repo นี้ใช้ **Frappe/ERPNext** ผ่าน Docker Compose (อยู่ในโฟลเดอร์ `frappe_docker/`) และเตรียมพื้นที่สำหรับทำ **FastAPI** (โฟลเดอร์ `fastapi/`).

## Quick start: รัน ERPNext (Docker)

Prerequisites:

- Docker Desktop + `docker compose`

คำสั่ง:

```powershell
cd .\frappe_docker
docker compose -f pwd.yml up -d

# รอสร้าง site (2-5 นาที)
docker compose -f pwd.yml logs -f create-site
```

เข้าใช้งาน:

- http://localhost:8080
- Username: `Administrator`
- Password: `admin`

ปิดระบบ:

```powershell
cd .\frappe_docker
docker compose -f pwd.yml down

# ถ้าจะล้างข้อมูลเริ่มใหม่ทั้งหมด
# docker compose -f pwd.yml down -v
```

## FastAPI

โฟลเดอร์ `fastapi/` ยังเป็นพื้นที่ว่างสำหรับทีมเพิ่มโค้ด/requirements เพิ่มเติม

## Avalonia (Desktop)

แนวทางที่ง่ายสุดคือทำ Avalonia เป็น **Desktop client** แล้วให้คุยกับ backend ผ่าน HTTP:

- Avalonia (UI) → เรียก FastAPI (integration/API layer)
- FastAPI → คุยกับ ERPNext/Frappe (REST API) หรือทำ business logic เพิ่ม

### Prerequisites

- ติดตั้ง **.NET SDK 8** (Windows)

ตรวจสอบ:

```powershell
dotnet --version
```

### Workflow ตอน dev (รันคู่กัน)

1) รัน ERPNext (Docker):

```powershell
cd .\frappe_docker
docker compose -f pwd.yml up -d
```

2) รัน FastAPI (ตัวอย่างแนวคิด):

```powershell
# สมมติว่าทีมสร้าง fastapi/app/main.py และ requirements.txt แล้ว
cd ..\fastapi
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

3) รัน Avalonia:

```powershell
# สมมติว่าทีมสร้างโปรเจคไว้ในโฟลเดอร์ avalonia/ แล้ว
dotnet run --project .\avalonia\Client\Client.csproj
```

หมายเหตุ:
- ERPNext demo เปิดที่ `http://localhost:8080`
- แนะนำให้ Avalonia เรียก FastAPI (เช่น `http://localhost:8000`) แล้วให้ FastAPI เป็นตัวกลางไป ERPNext เพื่อลดความซับซ้อนเรื่อง auth/CORS
