using ICalMonitor.Worker.Models;
using ICalMonitor.Worker.Services;
using Microsoft.Extensions.Options;

namespace ICalMonitor.Worker;

public class Worker(
    IOptions<AppConfig> options,
    IICalService icalService,
    ITelegramService telegramService,
    StateService stateService,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly AppConfig _config = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("iCal Booking Monitor started. Interval={Interval}m, Rooms={Count}",
            _config.IntervalMinutes, _config.Rooms.Count);

        stateService.Load();

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("=== Bắt đầu vòng quét lúc {Time} ===", DateTimeOffset.Now);

            foreach (var room in _config.Rooms)
            {
                await ScanRoomAsync(room, stoppingToken);
            }

            stateService.Save();

            logger.LogInformation("=== Hoàn thành vòng quét. Chờ {Interval} phút ===", _config.IntervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(_config.IntervalMinutes), stoppingToken);
        }
    }

    internal async Task ScanRoomAsync(RoomConfig room, CancellationToken ct)
    {
        logger.LogInformation("Quét phòng: {Room} ({Source})", room.Name, room.Source);

        List<BookingEvent> bookings;
        try
        {
            bookings = await icalService.FetchBookingsAsync(room.ICalUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi fetch iCal cho phòng {Room}, bỏ qua vòng này.", room.Name);
            return;
        }

        var isFirstRun = stateService.IsFirstRun(room.Name);
        if (isFirstRun)
        {
            logger.LogInformation("Phòng {Room}: lần đầu chạy — lưu {Count} UID làm baseline, không gửi notification.",
                room.Name, bookings.Count);
            stateService.AddUids(room.Name, bookings.Select(b => b.Uid));
            return;
        }

        var knownUids = stateService.GetKnownUids(room.Name);
        var newBookings = bookings.Where(b => !knownUids.Contains(b.Uid)).ToList();

        logger.LogInformation("Phòng {Room}: {Total} booking, {New} mới.",
            room.Name, bookings.Count, newBookings.Count);

        foreach (var booking in newBookings)
        {
            logger.LogInformation("  → Booking mới: uid={Uid}, check-in={CheckIn:dd/MM/yyyy}", booking.Uid, booking.Start);
            await telegramService.SendBookingAsync(booking, room, ct);
        }

        stateService.AddUids(room.Name, newBookings.Select(b => b.Uid));
    }
}
