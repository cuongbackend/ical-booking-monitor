using ICalMonitor.Worker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace ICalMonitor.Worker.Services;

public class TelegramService(IOptions<AppConfig> options, ILogger<TelegramService> logger) : ITelegramService
{
    private readonly TelegramConfig _cfg = options.Value.Telegram;

    public async Task SendBookingAsync(BookingEvent booking, RoomConfig room, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_cfg.BotToken) || _cfg.BotToken == "YOUR_BOT_TOKEN")
        {
            logger.LogWarning("Telegram BotToken chưa cấu hình — bỏ qua gửi notification.");
            return;
        }

        var message = BuildMessage(booking, room);

        try
        {
            var botClient = new TelegramBotClient(_cfg.BotToken);
            await botClient.SendMessage(
                chatId: _cfg.ChatId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            logger.LogInformation("Đã gửi Telegram notification: phòng={Room}, uid={Uid}", room.Name, booking.Uid);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi gửi Telegram: phòng={Room}, uid={Uid}", room.Name, booking.Uid);
        }
    }

    internal static string BuildMessage(BookingEvent booking, RoomConfig room)
    {
        var icon = room.Source?.ToLowerInvariant() switch
        {
            "airbnb" => "🏡",
            "dayladau" => "🏨",
            _ => "🏠"
        };

        var sourceLabel = room.Source?.ToLowerInvariant() switch
        {
            "airbnb" => "AirBnb",
            "dayladau" => "Dayladau",
            _ => room.Source ?? "Không rõ"
        };

        var nights = (booking.End.Date - booking.Start.Date).Days;
        var guestName = string.IsNullOrWhiteSpace(booking.Summary) ? "Chưa có tên" : booking.Summary;
        var shortUid = booking.Uid.Length > 16 ? booking.Uid[..16] : booking.Uid;

        return $"""
            {icon} <b>Booking mới — {EscapeHtml(room.Name)}</b>
            📋 Nguồn: {EscapeHtml(sourceLabel)}
            👤 Khách: {EscapeHtml(guestName)}
            📅 Check-in: {booking.Start:dd/MM/yyyy}
            📅 Check-out: {booking.End:dd/MM/yyyy}
            🌙 Số đêm: {nights}
            🆔 ID: {EscapeHtml(shortUid)}
            """;
    }

    private static string EscapeHtml(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
