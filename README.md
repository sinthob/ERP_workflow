# ERP Workflow System

A production-style ERP workflow system built with a **microservices architecture**, containerized with Docker, and integrated across three independent layers:
**ERPNext (backend)** → **FastAPI Gateway (middleware)** → **Avalonia Desktop UI (client)**

---

## What I Learned / Skills Demonstrated

### 🐳 Containerization & Docker

- Ran a **multi-container application** using `docker compose` with a custom network (`frappe_network`)
- Managed service dependencies, health checks, and inter-container communication via **hostname-based routing** (no hardcoded IPs)
- Built a custom Docker image for the Gateway API (`python:3.11-slim`) and integrated it into the compose stack
- Practiced separating concerns: each service lives in its own container, independently deployable

### 🔗 Microservices Integration

- Designed a **3-layer microservices architecture** where each layer communicates over HTTP REST
- Connected an external ERP platform (ERPNext/Frappe) as a backend service — treating it as a **third-party API** rather than owning the backend code
- Managed service discovery within Docker network: the Gateway talks to ERPNext via internal hostname `frontend:8080`, while the desktop client uses `localhost:8001`

### ⚙️ Middleware / API Gateway (FastAPI)

- Built a **Gateway API layer** in Python (FastAPI) that acts as a middleware between the ERP backend and the UI client
- Implemented full **CRUD endpoints** for Sales Orders, Work Orders, and Job Cards
- Designed a **workflow state machine** (`workflow_engine.py`) to manage Work Order status transitions with validation logic
- Used **Pydantic models** for strict request/response schema validation
- Applied **data normalization** (`normalization.py`) to adapt ERPNext's raw API responses into clean, consistent shapes for the client
- Secured credentials via environment variables (`.env`), never hardcoded
- Auto-generated **Swagger/OpenAPI docs** available at `/docs`

### ☁️ Cloud-Ready Practices

- All services are **containerized and stateless** — ready to be deployed to cloud platforms (AWS ECS, Azure Container Apps, GCP Cloud Run)
- Environment-based configuration (`ERP_BASE_URL`, `ERP_API_KEY`, `ERP_API_SECRET`) follows **12-Factor App** methodology
- Compose override files (`overrides/`) prepared for production scenarios (HTTPS, Traefik, custom domains, MariaDB secrets)

---

## Architecture

```
  Avalonia Desktop     avalonia_test/
  (.NET 8, Windows)
          │
          │  HTTP  localhost:8001
          ▼
    Gateway API        services/gateway-api/
  (FastAPI, Python)    Docker container — "operations-service"
          │
          │  HTTP  frontend:8080 (internal Docker network)
          ▼
  ERPNext / Frappe     frappe_docker/pwd.yml
  (Docker stack)       accessible from host at localhost:8081
```

All services run on a shared Docker network (`frappe_network`), enabling hostname-based routing between containers.

---

## Quick Start (วิธีแนะนำ Full Docker)

Prerequisites: **Docker Desktop** + `docker compose`

รันทุกอย่างพร้อมกันด้วยคำสั่งเดียว:

```powershell
cd .\frappe_docker
docker compose -f pwd.yml up -d --build
```

> `--build` จำเป็นครั้งแรก หรือทุกครั้งที่แก้ไขโค้ด gateway-api

รอสร้าง site (ครั้งแรกเท่านั้น ~3-5 นาที):

```powershell
docker compose -f pwd.yml logs -f create-site
# รอจนเห็น "Site frontend created" แล้ว Ctrl+C
```

เข้าใช้งาน:

| Service            | URL                                   |
| ------------------ | ------------------------------------- |
| ERPNext Web UI     | http://localhost:8081 (admin / admin) |
| Gateway API        | http://localhost:8001                 |
| API Docs (Swagger) | http://localhost:8001/docs            |

ปิดระบบ:

```powershell
cd .\frappe_docker
docker compose -f pwd.yml down

# ล้างข้อมูลทั้งหมด เริ่มใหม่
# docker compose -f pwd.yml down -v
```

---

## รัน Avalonia Desktop UI

Prerequisites: **.NET SDK 8**

```powershell
dotnet --version  # ตรวจสอบ
```

รัน (ต้องมี Docker stack รันอยู่ก่อน):

```powershell
cd .\avalonia_test
dotnet run
```

---

---

---

## โฟลเดอร์ที่ใช้งานจริง

> โฟลเดอร์ที่เป็น reference/examples ถูกย้ายไปรวมไว้ใต้ `only_for_reference/`

| โฟลเดอร์                                          | สถานะ     | หน้าที่                                      |
| ------------------------------------------------- | --------- | -------------------------------------------- |
| `frappe_docker/`                                  | ใช้จริง   | Docker Compose stack ของ ERPNext ทั้งหมด     |
| `services/gateway-api/`                           | ใช้จริง   | FastAPI middleware (port 8001)               |
| `avalonia_test/`                                  | ใช้จริง   | Avalonia Desktop UI (ตัวหลักของทีม)          |
| `only_for_reference/avalonia/Client/`             | reference | Avalonia เวอร์ชันเก่า (port 8003) ไม่ใช้แล้ว |
| `only_for_reference/services/operations-service/` | reference | Starter code เท่านั้น ไม่ได้ deploy          |
| `only_for_reference/examples/`                    | อ้างอิง   | สคริปต์ทดลอง/ตัวอย่าง                        |
| `only_for_reference/Example_Avalonia/`            | อ้างอิง   | ตัวอย่าง Avalonia เพิ่มเติม                  |

---

## Architecture

```

  Avalonia Desktop     avalonia_test/
  (.NET 8, Windows)

          HTTP  localhost:8001


    Gateway API        services/gateway-api/
  (FastAPI, Python)    รันเป็น Docker container ชื่อ "operations-service"

          HTTP  frontend:8080 (ภายใน Docker network)


  ERPNext / Frappe     frappe_docker/pwd.yml
  (Docker stack)       เข้าถึงจากเครื่อง host ที่ localhost:8081

```

**Docker stack ทั้งหมดรันด้วยคำสั่งเดียว** ERPNext และ Gateway API อยู่ใน network เดียวกัน (`frappe_network`) สามารถคุยกันด้วย hostname ได้เลย

---

## ไฟล์สำคัญ

### Backend `services/gateway-api/`

| ไฟล์                    | หน้าที่                                                      |
| ----------------------- | ------------------------------------------------------------ |
| `main.py`               | FastAPI application นิยาม routes ทั้งหมด                     |
| `team3_models.py`       | Pydantic models (SalesOrder, WorkOrder, JobCard)             |
| `erpnext_client.py`     | HTTP client สำหรับเชื่อมต่อ ERPNext REST API                 |
| `workflow_engine.py`    | Logic สำหรับ Work Order status transitions                   |
| `normalization.py`      | Data normalization helpers                                   |
| `schemas.py`            | Request/Response schemas เพิ่มเติม                           |
| `Dockerfile`            | สำหรับ build เป็น Docker image (python:3.11-slim, port 8001) |
| `.env` / `.env.example` | Config: `ERP_BASE_URL`, `ERP_API_KEY`, `ERP_API_SECRET`      |

### Frontend `avalonia_test/`

| ไฟล์                  | หน้าที่                                                          |
| --------------------- | ---------------------------------------------------------------- |
| `MainWindow.cs`       | หน้าต่างหลัก UI ทุกหน้า (SO list, WO list, Kanban, create forms) |
| `ApiClient.cs`        | HTTP client เรียก Gateway API ที่ `localhost:8001`               |
| `App.cs`              | Avalonia Application entry point                                 |
| `Program.cs`          | .NET Program entry point                                         |
| `AvaloniaTest.csproj` | Project file (.NET 8)                                            |

### Infrastructure `frappe_docker/`

| ไฟล์           | หน้าที่                                                           |
| -------------- | ----------------------------------------------------------------- |
| `pwd.yml`      | **Main compose file** full ERPNext stack + operations-service     |
| `compose.yaml` | Template compose file (ไม่มี DB/Redis ใช้เป็น reference เท่านั้น) |
| `overrides/`   | Compose override files สำหรับ production scenarios                |

---

## API Endpoints (Gateway API — port 8001)

| Method | Path                                | Description                      |
| ------ | ----------------------------------- | -------------------------------- |
| GET    | `/health`                           | Health check                     |
| GET    | `/erp/ping`                         | Test connection to ERPNext       |
| GET    | `/sales-orders`                     | List Sales Orders                |
| POST   | `/sales-orders`                     | Create Sales Order               |
| GET    | `/sales-orders/{name}`              | Get single Sales Order           |
| PUT    | `/sales-orders/{name}`              | Update Sales Order               |
| DELETE | `/sales-orders/{name}`              | Delete Sales Order               |
| GET    | `/work-orders`                      | List Work Orders                 |
| POST   | `/work-orders`                      | Create Work Order                |
| POST   | `/work-orders/{name}/transition`    | Trigger Work Order status change |
| GET    | `/job-cards`                        | List Job Cards                   |
| POST   | `/job-cards`                        | Create Job Card                  |
| GET    | `/views/orders/{so}/kanban`         | Kanban board for a Sales Order   |
| GET    | `/views/orders/{so}/timeline`       | Timeline for a Sales Order       |
| GET    | `/views/work-orders/{wo}/job-cards` | Job Cards within a Work Order    |

Swagger UI: http://localhost:8001/docs

---

## Desktop UI (Avalonia)

> **Note:** The Avalonia UI was built with heavy AI assistance. My primary focus and hands-on learning in this project was on the backend, middleware, and infrastructure layers above.

The desktop client (`avalonia_test/`) is a .NET 8 Windows app that consumes the Gateway API and provides views for Sales Orders, Work Orders, Job Cards, and a Kanban board.

```powershell
cd .\avalonia_test
dotnet run
```

Requires Docker stack to be running first.

---
