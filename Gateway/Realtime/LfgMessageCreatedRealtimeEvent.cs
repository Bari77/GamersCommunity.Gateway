namespace Gateway.Realtime;

public sealed class LfgMessageCreatedRealtimeEvent
{
    public string Type { get; init; } = RealtimeEventTypes.LfgMessageCreated;

    public required LfgMessageRealtimePayload Message { get; init; }
}

public sealed class LfgMessageRealtimePayload
{
    public Guid PublicId { get; init; }
    public string Body { get; init; } = "";
    public string SenderNickname { get; init; } = "";
    public string SenderDiscriminator { get; init; } = "";
    public Guid PlayerPublicId { get; init; }
    public Guid PlatformUserPublicId { get; init; }
    public string SenderAvatarUrl { get; init; } = "";
    public DateTime CreationDate { get; init; }
}
