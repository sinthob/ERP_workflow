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
