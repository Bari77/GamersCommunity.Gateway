namespace Gateway.Realtime;

public sealed class NotificationCreatedRealtimeEvent
{
    public string? Type { get; init; }
    public string? RecipientKeycloak { get; init; }
    public NotificationRealtimePayload? Notification { get; init; }
}

public sealed class NotificationRealtimePayload
{
    public int Id { get; init; }
    public Guid PublicId { get; init; }
    public int IdUser { get; init; }
    public string Kind { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Body { get; init; }
    public string? LinkUrl { get; init; }
    public bool IsRead { get; init; }
    public string? PayloadJson { get; init; }
    public DateTime CreationDate { get; init; }
}
