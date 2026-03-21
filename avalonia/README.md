# Avalonia (Desktop client)

โฟลเดอร์นี้ตั้งใจไว้สำหรับทำ Desktop app ด้วย Avalonia เพื่อใช้คู่กับ ERPNext (Frappe) และ/หรือ FastAPI

## Setup (Windows)

1. ติดตั้ง .NET SDK 8

ตรวจสอบ:

```powershell
dotnet --version
```

2. Restore + Run (โปรเจคตัวอย่างใน repo):

```powershell
cd .\avalonia
dotnet run --project .\Client\Client.csproj
```

หมายเหตุ: โปรเจคนี้เป็นแบบ code-only (ไม่มี XAML) เพื่อลดจำนวนไฟล์

## ถ้าต้องการสร้างโปรเจคใหม่ (ทางเลือก)

ติดตั้ง Avalonia templates:

```powershell
dotnet new install Avalonia.Templates
```

สร้างโปรเจค (ตัวอย่าง):

```powershell
dotnet new avalonia.app -o Client
```

รัน:

```powershell
dotnet run --project .\Client\Client.csproj
```

## Dev with ERPNext

- รัน ERPNext: `frappe_docker/pwd.yml` จะเปิดเว็บที่ `http://localhost:8080`
- รัน FastAPI (Gateway): ดู `services/gateway-api/README.md` (default `http://localhost:8000`)
- ตัวอย่าง UI ใน `Client` มีแท็บ:
  - `Diagnostics` (เรียก `/health`, `/erp/ping` ของ operations-service)
  - `Kanban` (เรียก `GET http://localhost:8003/kanban/board` และกด move สถานะได้)
  - `Timeline` (เรียก `GET http://localhost:8003/timeline/tasks`)

หมายเหตุ: Base URL ของ operations-service ถูกกำหนดไว้ในโค้ด `Client/MainWindow.cs` (default `http://localhost:8003`)
