namespace Gateway.Abstractions
{
    public interface IRabbitRpcClient
    {
        /// <summary>
        /// Envoie un message RPC vers RabbitMQ et retourne la réponse sous forme brute (string).
        /// </summary>
        Task<string> CallAsync(string queue, string payload, CancellationToken ct);
    }
}
