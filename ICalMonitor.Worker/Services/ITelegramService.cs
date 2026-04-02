using ICalMonitor.Worker.Models;

namespace ICalMonitor.Worker.Services;

public interface ITelegramService
{
    Task SendBookingAsync(BookingEvent booking, RoomConfig room, CancellationToken ct = default);
}
