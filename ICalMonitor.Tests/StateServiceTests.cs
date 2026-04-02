using ICalMonitor.Worker.Models;
using ICalMonitor.Worker.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ICalMonitor.Tests;

public class StateServiceTests
{
    private static StateService CreateService(string stateFilePath = "data/test-state.json")
    {
        var options = Options.Create(new AppConfig { StateFilePath = stateFilePath });
        return new StateService(options, NullLogger<StateService>.Instance);
    }

    [Fact]
    public void GetKnownUids_RoomNotInState_ReturnsEmpty()
    {
        var svc = CreateService();

        var result = svc.GetKnownUids("Phòng 999");

        Assert.Empty(result);
    }

    [Fact]
    public void AddUids_NewRoom_SavesCorrectly()
    {
        var svc = CreateService();

        svc.AddUids("Phòng A", ["uid-001", "uid-002"]);

        var result = svc.GetKnownUids("Phòng A");
        Assert.Equal(2, result.Count);
        Assert.Contains("uid-001", result);
        Assert.Contains("uid-002", result);
    }

    [Fact]
    public void AddUids_ExistingRoom_AppendsUids()
    {
        var svc = CreateService();
        svc.AddUids("Phòng B", ["uid-001"]);

        svc.AddUids("Phòng B", ["uid-002", "uid-003"]);

        var result = svc.GetKnownUids("Phòng B");
        Assert.Equal(3, result.Count);
        Assert.Contains("uid-001", result);
        Assert.Contains("uid-002", result);
        Assert.Contains("uid-003", result);
    }

    [Fact]
    public void AddUids_DuplicateUid_NotAddedTwice()
    {
        var svc = CreateService();
        svc.AddUids("Phòng C", ["uid-001"]);

        svc.AddUids("Phòng C", ["uid-001"]);

        var result = svc.GetKnownUids("Phòng C");
        Assert.Single(result);
    }

    [Fact]
    public void Load_FileNotExist_ReturnsEmptyState()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-state-{Guid.NewGuid()}.json");
        var svc = CreateService(tempPath);

        svc.Load(); // file không tồn tại

        Assert.Empty(svc.GetKnownUids("bất kỳ phòng nào"));
    }

    [Fact]
    public void IsFirstRun_RoomNotInState_ReturnsTrue()
    {
        var svc = CreateService();

        Assert.True(svc.IsFirstRun("Phòng mới"));
    }

    [Fact]
    public void IsFirstRun_RoomAlreadyAdded_ReturnsFalse()
    {
        var svc = CreateService();
        svc.AddUids("Phòng cũ", ["uid-x"]);

        Assert.False(svc.IsFirstRun("Phòng cũ"));
    }
}
