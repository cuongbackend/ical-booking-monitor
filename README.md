# iCal Booking Monitor

![CI](https://github.com/cuongnm/ical-booking-monitor/actions/workflows/ci.yml/badge.svg)

Ứng dụng tự động quét link iCal từ **AirBnb** và **Dayladau**, phát hiện booking mới và gửi thông báo ngay qua **Telegram**. Chạy 24/7 như Windows Service trên máy tính cá nhân.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Telegram Bot token — tạo qua [@BotFather](https://t.me/botfather)
- Chat ID của Telegram group/channel nhận thông báo

---

## Quick Start

```powershell
# 1. Clone repo
git clone https://github.com/cuongnm/ical-booking-monitor.git
cd ical-booking-monitor

# 2. Copy file cấu hình mẫu
cp ICalMonitor.Worker/appsettings.example.json ICalMonitor.Worker/appsettings.json

# 3. Điền thông tin thật vào appsettings.json
#    - Telegram.BotToken
#    - Telegram.ChatId
#    - Rooms[].ICalUrl

# 4. Chạy thử
dotnet run --project ICalMonitor.Worker
```

---

## Thêm phòng mới

Mở `ICalMonitor.Worker/appsettings.json`, thêm entry vào mảng `Rooms`:

```json
{
  "Monitor": {
    "Rooms": [
      {
        "Name": "Phòng 101 - AirBnb",
        "Source": "airbnb",
        "ICalUrl": "https://www.airbnb.com/calendar/ical/XXXX.ics"
      },
      {
        "Name": "Phòng 101 - Dayladau",
        "Source": "dayladau",
        "ICalUrl": "https://api.dayladau.com/v1/listings/XXXX/ical"
      }
    ]
  }
}
```

Sau đó restart service: `sc stop ICalMonitor && sc start ICalMonitor`

> **Lần đầu chạy với phòng mới:** ứng dụng sẽ lưu tất cả booking hiện có làm baseline mà **không gửi notification**, tránh spam.

---

## Deploy Windows Service

Xem hướng dẫn chi tiết tại [docs/deploy.md](docs/deploy.md).

Tóm tắt nhanh:

```powershell
# Publish
dotnet publish ICalMonitor.Worker -c Release -r win-x64 --self-contained -o ./publish

# Cài service (Admin PowerShell)
sc create "ICalMonitor" binPath="C:\<path>\publish\ICalMonitor.Worker.exe"
sc config "ICalMonitor" start=auto
sc start "ICalMonitor"
```

---

## Cấu trúc project

```
ical-booking-monitor/
├── ICalMonitor.sln
├── ICalMonitor.Worker/               # Worker Service chính
│   ├── Models/
│   │   ├── AppConfig.cs              # Cấu hình app, phòng, Telegram
│   │   └── BookingEvent.cs           # Model booking từ iCal
│   ├── Services/
│   │   ├── ICalService.cs            # Fetch & parse iCal URL
│   │   ├── StateService.cs           # Lưu/đọc UID đã biết
│   │   └── TelegramService.cs        # Gửi Telegram notification
│   ├── Worker.cs                     # Vòng lặp quét định kỳ
│   ├── Program.cs                    # DI, host setup
│   ├── appsettings.example.json      # Mẫu cấu hình (safe to commit)
│   └── appsettings.json              # Cấu hình thật (gitignored)
├── ICalMonitor.Tests/                # Unit tests (xUnit + Moq)
│   ├── StateServiceTests.cs
│   ├── TelegramServiceTests.cs
│   └── WorkerTests.cs
├── docs/
│   ├── idea.md
│   ├── prd.md
│   ├── spec.md
│   └── deploy.md
└── features/
    └── booking_monitor.feature       # BDD scenarios (Gherkin)
```

---

## Tech stack

| Thư viện | Vai trò |
|----------|---------|
| .NET 8 Worker Service | Background service host |
| [Ical.Net](https://github.com/rianjs/ical.net) | Parse iCal/ICS format |
| [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) | Gửi Telegram message |
| xUnit + Moq | Unit testing |
