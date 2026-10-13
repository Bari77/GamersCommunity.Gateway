namespace Gateway.Realtime;

public static class RealtimeGroups
{
    public const string WowLfgGlobal = "wow:lfg:global";
    public const string LolLfgGlobal = "lol:lfg:global";

    public static string User(string keycloakSubject)
    {
        if (Guid.TryParse(keycloakSubject, out var guid))
            return $"user:{guid:D}";

        return $"user:{keycloakSubject}";
    }
}
