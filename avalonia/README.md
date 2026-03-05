# Avalonia (Desktop client)

โฟลเดอร์นี้ตั้งใจไว้สำหรับทำ Desktop app ด้วย Avalonia เพื่อใช้คู่กับ ERPNext (Frappe) และ/หรือ FastAPI

## Setup (Windows)

1) ติดตั้ง .NET SDK 8
2) ติดตั้ง Avalonia templates:

```powershell
dotnet new install Avalonia.Templates
```

3) สร้างโปรเจค (ตัวอย่าง):

```powershell
# จาก repo root
mkdir avalonia
cd avalonia

dotnet new avalonia.app -o Client
```

4) รัน:

```powershell
dotnet run --project .\Client\Client.csproj
```

## Dev with ERPNext

- รัน ERPNext: `frappe_docker/pwd.yml` จะเปิดเว็บที่ `http://localhost:8080`
- แนะนำให้ UI คุยกับ FastAPI (เช่น `http://localhost:8000`) แล้วให้ FastAPI ไปคุยกับ ERPNext ต่อ
