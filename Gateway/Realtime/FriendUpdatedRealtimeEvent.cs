namespace Gateway.Realtime;

public sealed class FriendUpdatedRealtimeEvent
{
    public string? Type { get; init; }
    public string? AskingKeycloak { get; init; }
    public string? ReceivingKeycloak { get; init; }
    public int IdFriendAsking { get; init; }
    public int IdFriendReceive { get; init; }
    public int IdFriendStatus { get; init; }
    public Guid PublicId { get; init; }
}
