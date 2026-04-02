using System.Text.Json;
using ICalMonitor.Worker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ICalMonitor.Worker.Services;

public class StateService(IOptions<AppConfig> options, ILogger<StateService> logger)
{
    private readonly string _stateFilePath = options.Value.StateFilePath;
    private Dictionary<string, List<string>> _state = new();

    public void Load()
    {
        if (!File.Exists(_stateFilePath))
        {
            logger.LogInformation("State file không tồn tại, bắt đầu với state rỗng: {Path}", _stateFilePath);
            _state = new Dictionary<string, List<string>>();
            return;
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            _state = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                     ?? new Dictionary<string, List<string>>();
            logger.LogInformation("Loaded state: {RoomCount} phòng từ {Path}", _state.Count, _stateFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi đọc state file, reset về rỗng: {Path}", _stateFilePath);
            _state = new Dictionary<string, List<string>>();
        }
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
            logger.LogDebug("Saved state tới {Path}", _stateFilePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi ghi state file: {Path}", _stateFilePath);
        }
    }

    /// <summary>Trả về true nếu phòng chưa có trong state (lần đầu chạy).</summary>
    public bool IsFirstRun(string roomName) => !_state.ContainsKey(roomName);

    public List<string> GetKnownUids(string roomName)
        => _state.TryGetValue(roomName, out var uids) ? uids : new List<string>();

    public void AddUids(string roomName, IEnumerable<string> uids)
    {
        if (!_state.ContainsKey(roomName))
            _state[roomName] = new List<string>();

        foreach (var uid in uids)
            if (!_state[roomName].Contains(uid))
                _state[roomName].Add(uid);
    }
}
