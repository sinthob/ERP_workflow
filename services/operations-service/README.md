# operations-service (Team 3 – Operations & Workflow)

FastAPI microservice สำหรับทีม 3: คุม workflow/state transitions และเป็นตัวกลางคุยกับ ERPNext ผ่าน **REST API + Webhooks เท่านั้น** (ห้ามต่อ DB โดยตรง)

## Run (Windows)

```powershell
cd .\services\operations-service
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt

copy .env.example .env

# run
uvicorn main:app --reload --port 8003
```

## Run with Docker

```powershell
cd .\services\operations-service

docker build -t operations-service:dev .
docker run --rm -p 8003:8003 --env-file .env operations-service:dev
```

## Endpoints (starter)

- `GET /health`
- `GET /erp/ping` → ping ERPNext
- `GET /kanban/board` → columns + tasks grouped by status
- `POST /tasks/{name}/transition` → validate transition + update status in ERPNext
- `GET /timeline/tasks` → list tasks sorted by `modified` (simple timeline)
- `GET /kanban/generic-board?kind=task|work_order|job_card` → generic cards grouped by status (starter)
- `GET /timeline/cards?kind=task|work_order|job_card` → generic timeline list (starter)
- `POST /cards/{kind}/{name}/transition` → validate transitions; `apply=true` supported only for `kind=task` (starter)
- `GET /orders` → list Sales Orders (starter)
- `GET /orders/{sales_order}/tasks?task_kind=work_order` → list “tasks” for an order (starter; Work Order only)
- `GET /orders/{sales_order}/kanban?task_kind=work_order` → kanban board for an order (starter; Work Order only)
- `GET /work-orders/{work_order}/job-cards` → job card details under a work order (starter)
- `POST /webhooks/erpnext` → receiver for ERPNext webhooks (optional token)

## Config

อ่านจาก `.env`:

- `ERP_BASE_URL`
- `ERP_API_KEY`, `ERP_API_SECRET` (optional แต่แนะนำ)
- `WEBHOOK_TOKEN` (optional)
- `SERVICE_PORT` (default 8003)
