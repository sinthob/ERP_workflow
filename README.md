# ERP_workflow (local workspace)

Repo นี้ใช้ **Frappe/ERPNext** ผ่าน Docker Compose (อยู่ในโฟลเดอร์ `frappe_docker/`) และมี **Gateway API (FastAPI)** ที่ใช้งานจริงอยู่ใน `services/gateway-api/`.

## Services (ใช้จริง)

- `services/gateway-api/` : Gateway API ที่ Avalonia/Client เรียก
- `services/operations-service/` : Service สำหรับ workflow/kanban (เชื่อม ERPNext ผ่าน REST)

## Architecture (มือใหม่)

ภาพรวมแบบง่าย:

- **ERPNext/Frappe** (ระบบ ERP หลัก) รันด้วย Docker ใน `frappe_docker/`
- **Gateway API (FastAPI)** อยู่ที่ `services/gateway-api/` เป็น “ประตูหน้า” ให้ Client เรียก
- **Operations service** อยู่ที่ `services/operations-service/` ทำ logic เรื่อง workflow/kanban และคุยกับ ERPNext ผ่าน REST (ตอนนี้โฟกัส DocType `Task`)
- **Avalonia** อยู่ที่ `avalonia/` เป็น Desktop UI

Flow โดยทั่วไป:

`Avalonia (UI)` → `Gateway API` → (เรียก `operations-service` และ/หรือ `ERPNext`) → `ERPNext/Frappe`

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

- http://localhost:8081
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

FastAPI เป็นตัวกลางให้ Avalonia เรียกใช้งาน และสามารถไปคุยกับ ERPNext ต่อได้

รัน:

```powershell
cd .\services\gateway-api
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
copy .env.example .env
uvicorn main:app --reload --port 8000
```

ทดสอบ:

- http://localhost:8000/health
- http://localhost:8000/erp/ping

## Operations service (workflow/kanban)

รัน:

```powershell
cd .\services\operations-service
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
copy .env.example .env
uvicorn main:app --reload --port 8003
```

ทดสอบ:

- http://localhost:8003/health
- http://localhost:8003/kanban/board

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

1. รัน ERPNext (Docker):

```powershell
cd .\frappe_docker
docker compose -f pwd.yml up -d
```

2. รัน FastAPI (ตัวอย่างแนวคิด):

```powershell
cd .\services\gateway-api
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
copy .env.example .env
uvicorn main:app --reload --port 8000
```

3. รัน Avalonia:

```powershell
dotnet run --project .\avalonia\Client\Client.csproj
```

## Examples

โค้ดตัวอย่าง/สคริปต์ทดลองถูกย้ายไปไว้ที่ `examples/` เพื่อไม่ให้ปนกับ service ที่ใช้งานจริง

หมายเหตุ:

- ERPNext demo เปิดที่ `http://localhost:8081`
- แนะนำให้ Avalonia เรียก FastAPI (เช่น `http://localhost:8000`) แล้วให้ FastAPI เป็นตัวกลางไป ERPNext เพื่อลดความซับซ้อนเรื่อง auth/CORS
