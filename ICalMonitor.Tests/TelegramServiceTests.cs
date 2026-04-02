using ICalMonitor.Worker.Models;
using ICalMonitor.Worker.Services;

namespace ICalMonitor.Tests;

public class TelegramServiceTests
{
    private static BookingEvent MakeBooking(string summary = "Nguyen Van A",
        string uid = "airbnb-uid-001-2024",
        DateTime? start = null, DateTime? end = null) => new()
    {
        Uid = uid,
        Summary = summary,
        Start = start ?? new DateTime(2026, 5, 1),
        End = end ?? new DateTime(2026, 5, 4),
    };

    private static RoomConfig MakeRoom(string source = "airbnb", string name = "Phòng 101") => new()
    {
        Name = name,
        Source = source,
        ICalUrl = "https://example.com/cal.ics"
    };

    [Fact]
    public void FormatMessage_AirbnbSource_ContainsCorrectIcon()
    {
        var message = TelegramService.BuildMessage(MakeBooking(), MakeRoom("airbnb"));

        Assert.Contains("🏡", message);
    }

    [Fact]
    public void FormatMessage_DayladauSource_ContainsCorrectIcon()
    {
        var message = TelegramService.BuildMessage(MakeBooking(), MakeRoom("dayladau"));

        Assert.Contains("🏨", message);
    }

    [Fact]
    public void FormatMessage_UnknownSource_ContainsDefaultIcon()
    {
        var message = TelegramService.BuildMessage(MakeBooking(), MakeRoom("other"));

        Assert.Contains("🏠", message);
    }

    [Fact]
    public void FormatMessage_NoSummary_ShowsDefaultText()
    {
        var message = TelegramService.BuildMessage(MakeBooking(summary: ""), MakeRoom());

        Assert.Contains("Chưa có tên", message);
    }

    [Fact]
    public void FormatMessage_WithSummary_ShowsGuestName()
    {
        var message = TelegramService.BuildMessage(MakeBooking(summary: "Tran Thi B"), MakeRoom());

        Assert.Contains("Tran Thi B", message);
        Assert.DoesNotContain("Chưa có tên", message);
    }

    [Fact]
    public void FormatMessage_CalculatesNightsCorrectly()
    {
        var booking = MakeBooking(
            start: new DateTime(2026, 5, 1),
            end: new DateTime(2026, 5, 4));

        var message = TelegramService.BuildMessage(booking, MakeRoom());

        Assert.Contains("Số đêm: 3", message);
    }

    [Fact]
    public void FormatMessage_ContainsCheckInAndCheckOut()
    {
        var booking = MakeBooking(
            start: new DateTime(2026, 5, 1),
            end: new DateTime(2026, 5, 4));

        var message = TelegramService.BuildMessage(booking, MakeRoom());

        Assert.Contains("01/05/2026", message);
        Assert.Contains("04/05/2026", message);
    }

    [Fact]
    public void FormatMessage_LongUid_TruncatesTo16Chars()
    {
        var booking = MakeBooking(uid: "airbnb-uid-very-long-uid-12345");

        var message = TelegramService.BuildMessage(booking, MakeRoom());

        Assert.Contains("airbnb-uid-very-", message);
        Assert.DoesNotContain("airbnb-uid-very-long-uid-12345", message);
    }

    [Fact]
    public void FormatMessage_HtmlSpecialCharsInRoomName_AreEscaped()
    {
        var room = MakeRoom(name: "Phòng <101> & Suite");

        var message = TelegramService.BuildMessage(MakeBooking(), room);

        Assert.Contains("&lt;101&gt;", message);
        Assert.Contains("&amp;", message);
    }
}
