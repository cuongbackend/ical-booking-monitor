# Technical Specification — iCal Booking Monitor

## Kiến trúc hệ thống

Ứng dụng được xây dựng theo mô hình **Background Worker** (.NET 8 Worker Service):

```
┌─────────────────────────────────────────────────────┐
│                  Windows Service                    │
│                                                     │
│  ┌──────────────┐     ┌─────────────────────────┐  │
│  │ BookingMonitor│────▶│      ICalService        │  │
│  │   Worker     │     │  (fetch & parse iCal)   │  │
│  │ (10min loop) │     └─────────────────────────┘  │
│  │              │     ┌─────────────────────────┐  │
│  │              │────▶│      StateService       │  │
│  │              │     │  (load/save known UIDs) │  │
│  │              │     └─────────────────────────┘  │
│  │              │     ┌─────────────────────────┐  │
│  │              │────▶│    TelegramService      │  │
│  │              │     │   (send notification)   │  │
│  └──────────────┘     └─────────────────────────┘  │
└─────────────────────────────────────────────────────┘
         │                          │
    appsettings.json          state.json
    (cấu hình phòng,          (danh sách UID
     Telegram token)           đã biết)
```

**Luồng xử lý chính:**
1. Worker thức dậy mỗi 10 phút (cấu hình được).
2. Với mỗi phòng, gọi `ICalService` để fetch & parse tất cả iCal URL.
3. So sánh danh sách UID booking nhận được với state đã lưu (`StateService`).
4. Với mỗi UID mới, gọi `TelegramService` gửi thông báo.
5. Cập nhật state và lưu lại.
6. Lần đầu chạy (state rỗng cho phòng đó): lưu state nhưng không gửi notification.

---

## Data Models

### AppConfig
```csharp
public class AppConfig
{
    public int PollingIntervalMinutes { get; set; } = 10;
    public TelegramConfig Telegram { get; set; } = new();
    public List<RoomConfig> Rooms { get; set; } = new();
}
```

### RoomConfig
```csharp
public class RoomConfig
{
    // Tên phòng hiển thị trong thông báo
    public string Name { get; set; } = string.Empty;

    // Danh sách iCal export URL (AirBnb, Dayladau, ...)
    public List<string> ICalUrls { get; set; } = new();
}
```

### TelegramConfig
```csharp
public class TelegramConfig
{
    public string BotToken { get; set; } = string.Empty;
    public long ChatId { get; set; }
}
```

### BookingEvent
```csharp
public class BookingEvent
{
    public string Uid { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;   // tên khách / tiêu đề
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}
```

### RoomState (lưu trong state.json)
```csharp
public class RoomState
{
    // key = room name, value = set of known UIDs
    public Dictionary<string, HashSet<string>> KnownUids { get; set; } = new();
}
```

---

## Services

### ICalService
**Trách nhiệm:** Fetch file iCal từ URL và parse thành danh sách `BookingEvent`.

| Method | Signature | Mô tả |
|--------|-----------|-------|
| GetBookingsAsync | `Task<IReadOnlyList<BookingEvent>> GetBookingsAsync(string icalUrl, string roomName, CancellationToken ct)` | Fetch URL, parse VEVENT, bỏ qua CANCELLED, trả về danh sách booking. Ném exception nếu HTTP lỗi (caller xử lý). |

**Thư viện:** `Ical.Net` (NuGet).

---

### StateService
**Trách nhiệm:** Load/save trạng thái UID đã biết từ file `state.json`.

| Method | Signature | Mô tả |
|--------|-----------|-------|
| LoadAsync | `Task<RoomState> LoadAsync(CancellationToken ct)` | Đọc state.json, trả về RoomState (mới nếu file chưa tồn tại). |
| SaveAsync | `Task SaveAsync(RoomState state, CancellationToken ct)` | Ghi state.json (ghi đè). |
| IsFirstRun | `bool IsFirstRun(RoomState state, string roomName)` | Trả về true nếu phòng chưa có trong state (lần đầu sync). |

**Storage:** File JSON tại `data/state.json` bên cạnh executable.

---

### TelegramService
**Trách nhiệm:** Gửi thông báo booking mới qua Telegram Bot API.

| Method | Signature | Mô tả |
|--------|-----------|-------|
| SendNewBookingAsync | `Task SendNewBookingAsync(BookingEvent booking, CancellationToken ct)` | Format message và gửi đến ChatId đã cấu hình. |

**Message format:**
```
🏠 Phòng: {RoomName}
📅 Check-in:  {CheckIn:dd/MM/yyyy}
📅 Check-out: {CheckOut:dd/MM/yyyy}
👤 Khách: {Summary}
```

**Thư viện:** `Telegram.Bot` (NuGet).

---

## Cấu trúc thư mục ICalMonitor.Worker/

```
ICalMonitor.Worker/
├── ICalMonitor.Worker.csproj
├── Program.cs                        # Host builder, DI, Windows Service
├── Worker.cs                         # BackgroundService, vòng lặp chính
│
├── Config/
│   ├── AppConfig.cs
│   ├── RoomConfig.cs
│   └── TelegramConfig.cs
│
├── Models/
│   ├── BookingEvent.cs
│   └── RoomState.cs
│
├── Services/
│   ├── ICalService.cs
│   ├── StateService.cs
│   └── TelegramService.cs
│
├── appsettings.json                  # Cấu hình (không commit token)
├── appsettings.Development.json
│
└── data/
    └── state.json                    # Runtime state (gitignored)
```

---

## appsettings.json mẫu

```json
{
  "PollingIntervalMinutes": 10,
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN",
    "ChatId": 123456789
  },
  "Rooms": [
    {
      "Name": "Phòng 101 - AirBnb",
      "ICalUrls": [
        "https://www.airbnb.com/calendar/ical/XXXX.ics",
        "https://dayladau.com/ical/YYYY.ics"
      ]
    }
  ]
}
```

---

## Dependencies (NuGet)

| Package | Version | Mục đích |
|---------|---------|---------|
| Ical.Net | ≥ 4.x | Parse iCal/ICS format |
| Telegram.Bot | ≥ 19.x | Gửi Telegram message |
| Microsoft.Extensions.Hosting.WindowsServices | 8.x | Chạy như Windows Service |
| Serilog.Extensions.Hosting | latest | Structured logging |
| Serilog.Sinks.File | latest | Ghi log ra file |
