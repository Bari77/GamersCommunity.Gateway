namespace Gateway.Abstractions
{
    public interface IRabbitRpcClient
    {
        /// <summary>
        /// Sends an RPC message to a RabbitMQ queue and returns the raw response as a string.
        /// </summary>
        /// <param name="queue">
        /// The name of the target RabbitMQ queue.
        /// </param>
        /// <param name="payload">
        /// The message payload to send to the queue.
        /// </param>
        /// <param name="ct">
        /// A <see cref="CancellationToken"/> used to cancel the operation if needed.
        /// </param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> representing the asynchronous operation.  
        /// The result contains the raw response message as a string.
        /// </returns>
        Task<string> CallAsync(string queue, string payload, CancellationToken ct);
    }
}
