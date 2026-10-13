namespace Gateway.Realtime;

public sealed class MessageCreatedRealtimeEvent
{
    public string? Type { get; init; }
    public string[]? RecipientKeycloaks { get; init; }
    public MessageRealtimePayload? Message { get; init; }
}
