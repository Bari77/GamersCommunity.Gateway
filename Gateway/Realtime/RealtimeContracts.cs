namespace Gateway.Realtime;

public static class RealtimeQueues
{
    public const string Gateway = "gateway_realtime_queue";
}

public static class RealtimeEventTypes
{
    public const string MessageCreated = "message.created";
    public const string ConversationUpdated = "conversation.updated";
    public const string FriendUpdated = "friend.updated";
    public const string NotificationCreated = "notification.created";
    public const string ReportQueueUpdated = "report.queue.updated";
}

public static class RealtimeHubMethods
{
    public const string MessageCreated = "message.created";
    public const string ConversationUpdated = "conversation.updated";
    public const string FriendUpdated = "friend.updated";
    public const string NotificationCreated = "notification.created";
    public const string ReportQueueUpdated = "report.queue.updated";
}

public static class RealtimeGroups
{
    public static string User(string keycloakSubject)
    {
        if (Guid.TryParse(keycloakSubject, out var guid))
            return $"user:{guid:D}";

        return $"user:{keycloakSubject}";
    }
}
