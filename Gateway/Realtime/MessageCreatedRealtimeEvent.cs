namespace Gateway.Realtime;

public sealed class MessageCreatedRealtimeEvent
{
    public string? Type { get; init; }
    public string? SenderKeycloak { get; init; }
    public string? ReceiverKeycloak { get; init; }
    public MessageRealtimePayload? Message { get; init; }
}

public sealed class MessageRealtimePayload
{
    public Guid PublicId { get; init; }
    public string Content { get; init; } = "";
    public int IdSender { get; init; }
    public int IdReceiver { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreationDate { get; init; }
    public Guid? ParentPublicId { get; init; }
    public string? ParentContent { get; init; }
}
