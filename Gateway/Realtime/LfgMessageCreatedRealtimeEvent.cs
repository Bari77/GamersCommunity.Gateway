namespace Gateway.Realtime;

public sealed class LfgMessageCreatedRealtimeEvent
{
    public string Type { get; init; } = RealtimeEventTypes.LfgMessageCreated;

    public string? Game { get; init; }

    public required LfgMessageRealtimePayload Message { get; init; }
}
