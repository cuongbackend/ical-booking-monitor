using Ical.Net;
using Ical.Net.CalendarComponents;
using ICalMonitor.Worker.Models;
using Microsoft.Extensions.Logging;

namespace ICalMonitor.Worker.Services;

public class ICalService(IHttpClientFactory httpClientFactory, ILogger<ICalService> logger)
{
    public async Task<List<BookingEvent>> FetchBookingsAsync(string icalUrl)
    {
        var client = httpClientFactory.CreateClient("ical");

        string content;
        try
        {
            content = await client.GetStringAsync(icalUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không thể fetch iCal URL: {Url}", icalUrl);
            throw;
        }

        var calendar = Calendar.Load(content);
        var bookings = new List<BookingEvent>();

        if (calendar?.Events is null) return bookings;
        foreach (var evt in calendar.Events)
        {
            var status = evt.Status?.ToUpperInvariant();
            if (status == "CANCELLED")
            {
                logger.LogDebug("Bỏ qua CANCELLED event UID={Uid}", evt.Uid);
                continue;
            }

            bookings.Add(new BookingEvent
            {
                Uid = evt.Uid ?? string.Empty,
                Summary = evt.Summary ?? string.Empty,
                Start = evt.DtStart?.Value ?? default,
                End = evt.DtEnd?.Value ?? default,
            });
        }

        logger.LogDebug("Parsed {Count} booking(s) từ {Url}", bookings.Count, icalUrl);
        return bookings;
    }
}
