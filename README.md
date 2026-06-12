# ERP Workflow System

A production-style ERP workflow system built with a **microservices architecture**, containerized with Docker, and integrated across three independent layers:
**ERPNext (backend)** → **FastAPI Gateway (middleware)** → **Avalonia Desktop UI (client)**

---

## Summary (English)

### Problem

- Operations data and workflow actions were spread across systems, making it hard to enforce consistent business flow from UI to ERP.
- Calling ERP APIs directly from clients increases coupling and makes schema/workflow changes risky.
- The team needed a local-first setup that can still scale to cloud-style deployment patterns.

### Solution

- Built a 3-layer architecture: ERPNext backend, FastAPI Gateway middleware, and Avalonia Desktop client.
- Centralized validation, workflow transitions, and response normalization in the Gateway layer.
- Containerized services with Docker Compose on a shared network for predictable service-to-service communication.

### Tech Stack

- ERP Platform: ERPNext/Frappe
- Middleware/API: Python, FastAPI, Pydantic
- Desktop Client: .NET 8, Avalonia
- Infrastructure: Docker, Docker Compose
- API Contract: OpenAPI/Swagger
- Configuration: Environment variables (12-Factor aligned)

### Results

- Delivered an end-to-end workflow system from ERP backend through middleware to desktop UI.
- Reduced direct dependency between client and ERP by introducing an API Gateway boundary.

### Outcome / Impact (Measurable)

- Deployment consistency: a few commands stack startup for local environments.
- Change isolation: workflow rule updates can be implemented in Gateway without modifying ERP core.
- Integration clarity: normalized API responses reduced UI-side mapping complexity.
- Operational readiness: health checks and Swagger docs support faster debugging and onboarding.

### Design Decisions

- Why Gateway is separated from ERP:
  Keep business validation and API shaping in an owned middleware layer to decouple clients from ERP internals.
- Why hostname routing in Docker network:
  Service-name routing is stable across container restarts and avoids fragile hardcoded IP configuration.

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

## English Version (Expanded Guide)

## Quick Start (Recommended Full Docker Setup)

Prerequisites: **Docker Desktop** + `docker compose`

Run everything together with one command:

```powershell
cd .\frappe_docker
docker compose -f pwd.yml up -d --build
```

> `--build` is required on first run, or whenever gateway-api code changes.

Wait for site creation (first run only, around 3-5 minutes):

```powershell
docker compose -f pwd.yml logs -f create-site
# Wait until you see "Site frontend created", then press Ctrl+C
```

Access services:

| Service            | URL                                   |
| ------------------ | ------------------------------------- |
| ERPNext Web UI     | http://localhost:8081 (admin / admin) |
| Gateway API        | http://localhost:8001                 |
| API Docs (Swagger) | http://localhost:8001/docs            |

Stop the stack:

```powershell
cd .\frappe_docker
docker compose -f pwd.yml down

# Remove all volumes and start fresh
# docker compose -f pwd.yml down -v
```

---

## Run Avalonia Desktop UI

Prerequisites: **.NET SDK 8**

```powershell
dotnet --version  # check installed version
```

Run the app (Docker stack must already be running):

```powershell
cd .\avalonia_test
dotnet run
```

---

## Active Project Folders

> Reference/examples folders were moved under `only_for_reference/`.

| Folder                                            | Status    | Purpose                                             |
| ------------------------------------------------- | --------- | --------------------------------------------------- |
| `frappe_docker/`                                  | active    | Full ERPNext Docker Compose stack                   |
| `services/gateway-api/`                           | active    | FastAPI middleware (port 8001)                      |
| `avalonia_test/`                                  | active    | Main Avalonia Desktop UI used by the team           |
| `only_for_reference/avalonia/Client/`             | reference | Legacy Avalonia version (port 8003), no longer used |
| `only_for_reference/services/operations-service/` | reference | Starter code only, not deployed                     |
| `only_for_reference/examples/`                    | reference | Experimental scripts and sample files               |
| `only_for_reference/Example_Avalonia/`            | reference | Additional Avalonia examples                        |

---

## Architecture (Detailed)

```

  Avalonia Desktop     avalonia_test/
  (.NET 8, Windows)

          HTTP  localhost:8001


    Gateway API        services/gateway-api/
  (FastAPI, Python)    Runs as Docker container named "operations-service"

          HTTP  frontend:8080 (inside Docker network)


  ERPNext / Frappe     frappe_docker/pwd.yml
  (Docker stack)       Accessible from host at localhost:8081

```

**The full Docker stack runs with one command.** ERPNext and Gateway API are on the same network (`frappe_network`) and communicate by hostname.

---

## Key Files

### Backend `services/gateway-api/`

| File                    | Purpose                                                        |
| ----------------------- | -------------------------------------------------------------- |
| `main.py`               | FastAPI application and route definitions                      |
| `team3_models.py`       | Pydantic models (SalesOrder, WorkOrder, JobCard)               |
| `erpnext_client.py`     | HTTP client for ERPNext REST API                               |
| `workflow_engine.py`    | Work Order status transition logic                             |
| `normalization.py`      | Data normalization helper functions                            |
| `schemas.py`            | Additional request/response schemas                            |
| `Dockerfile`            | Docker image build file (python:3.11-slim, port 8001)          |
| `.env` / `.env.example` | Config values: `ERP_BASE_URL`, `ERP_API_KEY`, `ERP_API_SECRET` |

### Frontend `avalonia_test/`

| File                  | Purpose                                                 |
| --------------------- | ------------------------------------------------------- |
| `MainWindow.cs`       | Main UI window (SO list, WO list, Kanban, create forms) |
| `ApiClient.cs`        | HTTP client for Gateway API at `localhost:8001`         |
| `App.cs`              | Avalonia application entry point                        |
| `Program.cs`          | .NET program entry point                                |
| `AvaloniaTest.csproj` | Project file (.NET 8)                                   |

### Infrastructure `frappe_docker/`

| File           | Purpose                                                           |
| -------------- | ----------------------------------------------------------------- |
| `pwd.yml`      | **Main compose file** for full ERPNext stack + operations-service |
| `compose.yaml` | Template compose file (no DB/Redis, reference only)               |
| `overrides/`   | Compose override files for production scenarios                   |

---

## API Endpoints (Gateway API - port 8001)

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

## Desktop UI (Avalonia)

> **Note:** The Avalonia UI was built with heavy AI assistance. My primary focus and hands-on learning in this project was on the backend, middleware, and infrastructure layers above.

The desktop client (`avalonia_test/`) is a .NET 8 Windows app that consumes the Gateway API and provides views for Sales Orders, Work Orders, Job Cards, and a Kanban board.

```powershell
cd .\avalonia_test
dotnet run
```

Requires Docker stack to be running first.

---

---

## สรุป

### Problem (ปัญหา)

- ข้อมูลและขั้นตอนงานจากฝั่งปฏิบัติการกระจายอยู่หลายจุด ทำให้ควบคุม workflow ให้สอดคล้องตั้งแต่ UI ถึง ERP ได้ยาก
- ถ้าให้ client เรียก ERP โดยตรงจะเกิด coupling สูง และเสี่ยงเวลาปรับ schema หรือ business flow
- ทีมต้องการระบบที่รันในเครื่องได้ง่าย แต่ต่อยอดสู่รูปแบบการ deploy บน cloud ได้

### Solution (แนวทางแก้)

- ออกแบบระบบ 3 ชั้น: ERPNext backend, FastAPI Gateway middleware และ Avalonia Desktop client
- รวม validation, workflow transitions และ data normalization ไว้ที่ Gateway
- ทำ containerization ด้วย Docker Compose บน network เดียว เพื่อให้ service คุยกันได้เสถียร

### Tech Stack

- ERP Platform: ERPNext/Frappe
- Middleware/API: Python, FastAPI, Pydantic
- Desktop Client: .NET 8, Avalonia
- Infrastructure: Docker, Docker Compose
- API Contract: OpenAPI/Swagger
- Configuration: Environment variables ตามแนวคิด 12-Factor

### ผลลัพธ์

- ได้ระบบ workflow แบบ end-to-end ครบจาก ERP ผ่าน middleware ไปยัง desktop UI
- ลดการผูกติดโดยตรงระหว่าง client กับ ERP ด้วยการมี Gateway เป็นขอบเขตกลาง

### Outcome / Impact (วัดผลได้)

- ความสม่ำเสมอในการ deploy: เริ่ม stack ได้โดยไม่กี่คำสั่งผ่าน docker compose
- แยกผลกระทบการเปลี่ยนแปลง: ปรับกฎ workflow ที่ Gateway ได้โดยไม่แตะ ERP core
- ความชัดเจนของ integration: response ที่ normalize แล้วช่วยลดความซับซ้อนฝั่ง UI
- ความพร้อมใช้งานจริง: มี health checks และ Swagger ช่วย onboarding และ debug ได้เร็ว

### Architectural Decisions (ทำไมเลือกแบบนี้)

- ทำไมแยก Gateway ออกจาก ERP:
  เพื่อเก็บ logic ที่ทีมควบคุมเอง (validation, mapping, workflow) และลดการผูกกับโครงสร้างภายใน ERP
  เป็นการจำลองการเชื่อมต่อกับระบบ third-party
- เหตุผลที่ใช้ hostname routing ใน Docker network:
  ลดปัญหา IP เปลี่ยนตาม lifecycle ของ container และทำให้ config เสถียร/ดูแลง่ายกว่า

---

## เวอร์ชันภาษาไทย

### สิ่งที่ได้เรียนรู้ / ทักษะที่แสดงในโปรเจกต์นี้

### 🐳 การทำ Containerization และ Docker

- รันแอปพลิเคชันแบบหลาย container ด้วย `docker compose` และใช้ network เฉพาะของโปรเจกต์ (`frappe_network`)
- จัดการ dependency ของ service, health check, และการสื่อสารระหว่าง container ด้วยการเรียกผ่าน hostname แทนการ hardcode IP
- สร้าง Docker image แบบกำหนดเองสำหรับ Gateway API (`python:3.11-slim`) และนำไปเชื่อมกับ compose stack
- แยกหน้าที่ของแต่ละ service ออกจากกัน ทำให้แต่ละส่วน deploy ได้อย่างอิสระ

### 🔗 การเชื่อมต่อแบบ Microservices

- ออกแบบสถาปัตยกรรม microservices แบบ 3 ชั้น โดยแต่ละชั้นสื่อสารกันผ่าน HTTP REST
- เชื่อม ERP platform ภายนอกอย่าง ERPNext/Frappe เป็น backend service โดยมองเป็น **third-party API** แทนที่จะดูแลโค้ด backend เอง
- จัดการ service discovery ภายใน Docker network: Gateway เรียก ERPNext ผ่าน hostname ภายใน `frontend:8080` ส่วน desktop client ใช้ `localhost:8001`

### ⚙️ Middleware / API Gateway (FastAPI)

- สร้าง Gateway API layer ด้วย Python (FastAPI) ให้ทำหน้าที่เป็น middleware ระหว่าง ERP backend กับ UI client
- ทำ CRUD endpoints ครบสำหรับ Sales Orders, Work Orders และ Job Cards
- ออกแบบ workflow state machine (`workflow_engine.py`) เพื่อควบคุมการเปลี่ยนสถานะของ Work Order พร้อม validation logic
- ใช้ Pydantic models สำหรับตรวจสอบ schema ของ request/response อย่างเข้มงวด
- ทำ data normalization (`normalization.py`) เพื่อแปลงข้อมูลดิบจาก ERPNext ให้เป็นรูปแบบที่สะอาดและสม่ำเสมอสำหรับ client
- เก็บ credentials ไว้ใน environment variables (`.env`) ไม่ hardcode ลงในโค้ด
- มี Swagger/OpenAPI docs อัตโนมัติที่ `/docs`

### ☁️ แนวทางที่พร้อมต่อการขึ้น Cloud

- ทุก service ถูกทำให้เป็น container และ stateless จึงพร้อมนำไป deploy บน cloud platforms เช่น AWS ECS, Azure Container Apps และ GCP Cloud Run
- ใช้ environment-based configuration (`ERP_BASE_URL`, `ERP_API_KEY`, `ERP_API_SECRET`) ตามแนวทาง **12-Factor App**

---

## สถาปัตยกรรม

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

ทุก service รันอยู่บน Docker network เดียวกัน (`frappe_network`) จึงสามารถสื่อสารกันด้วย hostname ได้

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

## API Endpoints (เวอร์ชันภาษาไทย — Gateway API พอร์ต 8001)

| Method | Path                                | คำอธิบาย                             |
| ------ | ----------------------------------- | ------------------------------------ |
| GET    | `/health`                           | ตรวจสอบสถานะระบบ (Health check)      |
| GET    | `/erp/ping`                         | ทดสอบการเชื่อมต่อกับ ERPNext         |
| GET    | `/sales-orders`                     | ดึงรายการ Sales Orders               |
| POST   | `/sales-orders`                     | สร้าง Sales Order ใหม่               |
| GET    | `/sales-orders/{name}`              | ดึงข้อมูล Sales Order รายการเดียว    |
| PUT    | `/sales-orders/{name}`              | อัปเดต Sales Order                   |
| DELETE | `/sales-orders/{name}`              | ลบ Sales Order                       |
| GET    | `/work-orders`                      | ดึงรายการ Work Orders                |
| POST   | `/work-orders`                      | สร้าง Work Order ใหม่                |
| POST   | `/work-orders/{name}/transition`    | เปลี่ยนสถานะ Work Order ตาม workflow |
| GET    | `/job-cards`                        | ดึงรายการ Job Cards                  |
| POST   | `/job-cards`                        | สร้าง Job Card ใหม่                  |
| GET    | `/views/orders/{so}/kanban`         | แสดง Kanban board ของ Sales Order    |
| GET    | `/views/orders/{so}/timeline`       | แสดง Timeline ของ Sales Order        |
| GET    | `/views/work-orders/{wo}/job-cards` | แสดง Job Cards ภายใน Work Order      |

Swagger UI: http://localhost:8001/docs

---

## เกี่ยวกับ Desktop UI (Avalonia)

> **หมายเหตุ:** ส่วน Avalonia UI ถูกช่วยสร้างด้วย AI ค่อนข้างมาก โดยจุดที่ผมโฟกัสและได้ลงมือเรียนรู้จริงในโปรเจกต์นี้คือ backend, middleware และ infrastructure ด้านบน

Desktop client (`avalonia_test/`) เป็นแอป Windows บน .NET 8 ที่เรียกใช้ Gateway API และแสดงข้อมูล Sales Orders, Work Orders, Job Cards รวมถึง Kanban board

```powershell
cd .\avalonia_test
dotnet run
```

ต้องรัน Docker stack ให้พร้อมก่อน

---
