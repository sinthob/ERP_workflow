# ERP_workflow

ระบบ ERP Desktop ที่ประกอบด้วย **Frappe/ERPNext** เป็น backend, **Gateway API (FastAPI)** เป็น middleware และ **Avalonia** เป็น Desktop UI

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

| โฟลเดอร์                       | สถานะ     | หน้าที่                                      |
| ------------------------------ | --------- | -------------------------------------------- |
| `frappe_docker/`               | ใช้จริง   | Docker Compose stack ของ ERPNext ทั้งหมด     |
| `services/gateway-api/`        | ใช้จริง   | FastAPI middleware (port 8001)               |
| `avalonia_test/`               | ใช้จริง   | Avalonia Desktop UI (ตัวหลักของทีม)          |
| `avalonia/Client/`             | reference | Avalonia เวอร์ชันเก่า (port 8003) ไม่ใช้แล้ว |
| `services/operations-service/` | reference | Starter code เท่านั้น ไม่ได้ deploy          |
| `examples/`                    | อ้างอิง   | สคริปต์ทดลอง/ตัวอย่าง                        |

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

## API Endpoints (Gateway API port 8001)

| Method | Path                                | หน้าที่                        |
| ------ | ----------------------------------- | ------------------------------ |
| GET    | `/health`                           | Health check                   |
| GET    | `/erp/ping`                         | ทดสอบ connection ไปยัง ERPNext |
| GET    | `/sales-orders`                     | ดึงรายการ Sales Orders         |
| POST   | `/sales-orders`                     | สร้าง Sales Order ใหม่         |
| GET    | `/sales-orders/{name}`              | ดึง Sales Order เดียว          |
| PUT    | `/sales-orders/{name}`              | อัปเดต Sales Order             |
| DELETE | `/sales-orders/{name}`              | ลบ Sales Order                 |
| GET    | `/work-orders`                      | ดึงรายการ Work Orders          |
| POST   | `/work-orders`                      | สร้าง Work Order ใหม่          |
| POST   | `/work-orders/{name}/transition`    | เปลี่ยน status ของ Work Order  |
| GET    | `/job-cards`                        | ดึงรายการ Job Cards            |
| POST   | `/job-cards`                        | สร้าง Job Card ใหม่            |
| GET    | `/views/orders/{so}/kanban`         | Kanban board ของ Sales Order   |
| GET    | `/views/orders/{so}/timeline`       | Timeline ของ Sales Order       |
| GET    | `/views/work-orders/{wo}/job-cards` | Job Cards ใน Work Order        |

Swagger UI: http://localhost:8001/docs
