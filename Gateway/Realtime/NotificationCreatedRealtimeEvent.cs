namespace Gateway.Realtime;

public sealed class NotificationCreatedRealtimeEvent
{
    public string? Type { get; init; }
    public string? RecipientKeycloak { get; init; }
    public NotificationRealtimePayload? Notification { get; init; }
}
