namespace Gateway.Realtime;

public sealed class MessageCreatedRealtimeEvent
{
    public string? Type { get; init; }
    public string[]? RecipientKeycloaks { get; init; }
    public MessageRealtimePayload? Message { get; init; }
}

public sealed class MessageRealtimePayload
{
    public Guid PublicId { get; init; }
    public Guid ConversationPublicId { get; init; }
    public string Content { get; init; } = "";
    public int IdSender { get; init; }
    public Guid SenderPublicId { get; init; }
    public string SenderNickname { get; init; } = "";
    public string SenderDiscriminator { get; init; } = "";
    public string SenderAvatarUrl { get; init; } = "";
    public DateTime CreationDate { get; init; }
    public string Kind { get; init; } = "text";
    public Guid? ParentPublicId { get; init; }
    public string? ParentContent { get; init; }
}

public sealed class ConversationUpdatedRealtimeEvent
{
    public string? Type { get; init; }
    public string[]? RecipientKeycloaks { get; init; }
    public Guid ConversationPublicId { get; init; }
    public bool Deleted { get; init; }
}
