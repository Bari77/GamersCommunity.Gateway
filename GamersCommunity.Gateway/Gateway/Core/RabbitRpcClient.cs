using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;

namespace Gateway.Core
{
    /// <summary>
    /// Default implementation of <see cref="IRabbitRpcClient"/> that performs
    /// RPC-style messaging over RabbitMQ using a <see cref="RabbitMQProducer"/>.
    /// </summary>
    /// <remarks>
    /// This class uses a producer to send a request message to a queue and then waits
    /// for the correlated response, effectively simulating synchronous RPC over RabbitMQ.
    /// </remarks>
    public sealed class RabbitRpcClient(RabbitMQProducer producer) : IRabbitRpcClient
    {
        /// <inheritdoc />
        public async Task<string> CallAsync(string queue, string payload, CancellationToken ct = default)
        {
            // Send the request message and retrieve message properties (e.g., correlation ID).
            var props = await producer.SendMessageAsync(queue, payload, ct);

            // Wait for and return the correlated response message.
            return await producer.GetResponseAsync(props, ct);
        }
    }
}
