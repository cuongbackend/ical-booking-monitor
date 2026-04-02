# Deployment Guide — Windows Service

## Publish

```powershell
dotnet publish ICalMonitor.Worker -c Release -r win-x64 --self-contained -o ./publish
```

## Cài Windows Service

Chạy PowerShell với quyền **Administrator**:

```powershell
sc create "ICalMonitor" binPath="C:\<đường dẫn đầy đủ>\publish\ICalMonitor.Worker.exe"
sc config "ICalMonitor" start=auto
sc start "ICalMonitor"
```

> Thay `C:\<đường dẫn đầy đủ>` bằng đường dẫn thực tế, ví dụ:
> `C:\Apps\ical-booking-monitor\publish\ICalMonitor.Worker.exe`

## Xem log

```powershell
Get-EventLog -LogName Application -Source "ICalMonitor" -Newest 20
```

Hoặc xem trực tiếp file log (nếu cấu hình Serilog ghi file):

```powershell
Get-Content .\logs\log-.txt -Tail 50 -Wait
```

## Kiểm tra trạng thái service

```powershell
sc query "ICalMonitor"
```

## Update code

```powershell
sc stop "ICalMonitor"
git pull
dotnet publish ICalMonitor.Worker -c Release -r win-x64 --self-contained -o ./publish
sc start "ICalMonitor"
```

## Gỡ cài đặt

```powershell
sc stop "ICalMonitor"
sc delete "ICalMonitor"
```

## Cấu hình appsettings.json

File `ICalMonitor.Worker/appsettings.json` (không được commit lên git):

```json
{
  "Monitor": {
    "IntervalMinutes": 10,
    "StateFilePath": "data/state.json",
    "Telegram": {
      "BotToken": "1234567890:AAF...",
      "ChatId": "-100123456789"
    },
    "Rooms": [
      {
        "Name": "Phòng 101 - AirBnb",
        "Source": "airbnb",
        "ICalUrl": "https://www.airbnb.com/calendar/ical/XXXX.ics"
      }
    ]
  }
}
```

Xem mẫu đầy đủ tại [`appsettings.example.json`](../ICalMonitor.Worker/appsettings.example.json).
