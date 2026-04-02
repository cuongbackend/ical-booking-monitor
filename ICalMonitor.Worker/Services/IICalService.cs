using ICalMonitor.Worker.Models;

namespace ICalMonitor.Worker.Services;

public interface IICalService
{
    Task<List<BookingEvent>> FetchBookingsAsync(string icalUrl);
}
