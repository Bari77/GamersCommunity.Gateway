namespace Gateway.Realtime;

public sealed class ReportQueueUpdatedRealtimeEvent
{
    public string? Type { get; init; }
    public string[]? RecipientKeycloaks { get; init; }
    public int OpenCount { get; init; }
}
