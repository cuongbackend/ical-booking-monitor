namespace ICalMonitor.Worker.Models;

public class AppConfig
{
    public int IntervalMinutes { get; set; } = 10;
    public string StateFilePath { get; set; } = "data/state.json";
    public TelegramConfig Telegram { get; set; } = new();
    public List<RoomConfig> Rooms { get; set; } = new();
}

public class TelegramConfig
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

public class RoomConfig
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string ICalUrl { get; set; } = string.Empty;
}
