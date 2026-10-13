namespace Gateway.Realtime;

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
