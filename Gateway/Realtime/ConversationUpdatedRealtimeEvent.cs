namespace Gateway.Realtime;

public sealed class ConversationUpdatedRealtimeEvent
{
    public string? Type { get; init; }
    public string[]? RecipientKeycloaks { get; init; }
    public Guid ConversationPublicId { get; init; }
    public bool Deleted { get; init; }
}
