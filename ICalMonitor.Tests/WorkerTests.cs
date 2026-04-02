using ICalMonitor.Worker.Models;
using WorkerService = ICalMonitor.Worker.Worker;
using ICalMonitor.Worker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ICalMonitor.Tests;

public class WorkerTests
{
    private readonly Mock<IICalService> _icalMock = new();
    private readonly Mock<ITelegramService> _telegramMock = new();

    private (WorkerService worker, StateService state) CreateWorker(
        RoomConfig? room = null,
        string stateFilePath = "")
    {
        room ??= new RoomConfig { Name = "Phòng Test", Source = "airbnb", ICalUrl = "https://example.com/cal.ics" };

        var tempPath = string.IsNullOrEmpty(stateFilePath)
            ? Path.Combine(Path.GetTempPath(), $"worker-test-state-{Guid.NewGuid()}.json")
            : stateFilePath;

        var config = new AppConfig
        {
            IntervalMinutes = 10,
            StateFilePath = tempPath,
            Rooms = [room]
        };

        var state = new StateService(
            Options.Create(config),
            NullLogger<StateService>.Instance);

        var worker = new WorkerService(
            Options.Create(config),
            _icalMock.Object,
            _telegramMock.Object,
            state,
            NullLogger<WorkerService>.Instance);

        return (worker, state);
    }

    [Fact]
    public async Task ScanRoom_NewBooking_SendsTelegramNotification()
    {
        var room = new RoomConfig { Name = "Phòng A", Source = "airbnb", ICalUrl = "https://example.com/a.ics" };
        var (worker, state) = CreateWorker(room);

        // Simulate phòng đã có state (không phải first run)
        state.AddUids("Phòng A", ["existing-uid"]);

        var newBooking = new BookingEvent
        {
            Uid = "new-uid-001",
            Summary = "Nguyen Van A",
            Start = new DateTime(2026, 5, 1),
            End = new DateTime(2026, 5, 3)
        };

        _icalMock.Setup(s => s.FetchBookingsAsync(room.ICalUrl))
            .ReturnsAsync([newBooking]);

        _telegramMock.Setup(s => s.SendBookingAsync(
                It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await worker.ScanRoomAsync(room, CancellationToken.None);

        _telegramMock.Verify(s => s.SendBookingAsync(
            It.Is<BookingEvent>(b => b.Uid == "new-uid-001"),
            It.Is<RoomConfig>(r => r.Name == "Phòng A"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScanRoom_KnownUid_DoesNotSendDuplicate()
    {
        var room = new RoomConfig { Name = "Phòng B", Source = "airbnb", ICalUrl = "https://example.com/b.ics" };
        var (worker, state) = CreateWorker(room);

        // UID đã biết
        state.AddUids("Phòng B", ["known-uid-001"]);

        _icalMock.Setup(s => s.FetchBookingsAsync(room.ICalUrl))
            .ReturnsAsync([new BookingEvent { Uid = "known-uid-001", Summary = "Guest", Start = DateTime.Today, End = DateTime.Today.AddDays(2) }]);

        await worker.ScanRoomAsync(room, CancellationToken.None);

        _telegramMock.Verify(s => s.SendBookingAsync(
            It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanRoom_UrlUnreachable_LogsErrorAndContinues()
    {
        var room = new RoomConfig { Name = "Phòng C", Source = "dayladau", ICalUrl = "https://invalid.example.com/cal.ics" };
        var (worker, state) = CreateWorker(room);

        _icalMock.Setup(s => s.FetchBookingsAsync(room.ICalUrl))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Không được throw ra ngoài
        var ex = await Record.ExceptionAsync(() => worker.ScanRoomAsync(room, CancellationToken.None));

        Assert.Null(ex);
        _telegramMock.Verify(s => s.SendBookingAsync(
            It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanRoom_CancelledBooking_IsIgnored()
    {
        // ICalService đã lọc CANCELLED — Worker nhận list rỗng
        var room = new RoomConfig { Name = "Phòng D", Source = "airbnb", ICalUrl = "https://example.com/d.ics" };
        var (worker, state) = CreateWorker(room);

        state.AddUids("Phòng D", ["some-old-uid"]); // không phải first run

        _icalMock.Setup(s => s.FetchBookingsAsync(room.ICalUrl))
            .ReturnsAsync([]); // CANCELLED đã bị ICalService lọc → trả rỗng

        await worker.ScanRoomAsync(room, CancellationToken.None);

        _telegramMock.Verify(s => s.SendBookingAsync(
            It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScanRoom_FirstRun_DoesNotSendNotification()
    {
        var room = new RoomConfig { Name = "Phòng E", Source = "airbnb", ICalUrl = "https://example.com/e.ics" };
        var (worker, state) = CreateWorker(room);
        // state rỗng → IsFirstRun = true

        _icalMock.Setup(s => s.FetchBookingsAsync(room.ICalUrl))
            .ReturnsAsync([
                new BookingEvent { Uid = "uid-1", Summary = "Guest 1", Start = DateTime.Today, End = DateTime.Today.AddDays(1) },
                new BookingEvent { Uid = "uid-2", Summary = "Guest 2", Start = DateTime.Today.AddDays(2), End = DateTime.Today.AddDays(4) }
            ]);

        await worker.ScanRoomAsync(room, CancellationToken.None);

        _telegramMock.Verify(s => s.SendBookingAsync(
            It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // UIDs được lưu làm baseline
        var known = state.GetKnownUids("Phòng E");
        Assert.Contains("uid-1", known);
        Assert.Contains("uid-2", known);
    }

    [Fact]
    public async Task ScanRoom_MultipleNewBookings_SendsAllNotifications()
    {
        var room = new RoomConfig { Name = "Phòng F", Source = "dayladau", ICalUrl = "https://example.com/f.ics" };
        var (worker, state) = CreateWorker(room);
        state.AddUids("Phòng F", ["old-uid"]); // không phải first run

        _icalMock.Setup(s => s.FetchBookingsAsync(room.ICalUrl))
            .ReturnsAsync([
                new BookingEvent { Uid = "new-uid-1", Summary = "G1", Start = DateTime.Today, End = DateTime.Today.AddDays(1) },
                new BookingEvent { Uid = "new-uid-2", Summary = "G2", Start = DateTime.Today.AddDays(2), End = DateTime.Today.AddDays(3) },
                new BookingEvent { Uid = "new-uid-3", Summary = "G3", Start = DateTime.Today.AddDays(4), End = DateTime.Today.AddDays(5) },
            ]);

        _telegramMock.Setup(s => s.SendBookingAsync(
                It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await worker.ScanRoomAsync(room, CancellationToken.None);

        _telegramMock.Verify(s => s.SendBookingAsync(
            It.IsAny<BookingEvent>(), It.IsAny<RoomConfig>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }
}
