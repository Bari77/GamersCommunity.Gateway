using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;

namespace Gateway.Core
{
    public sealed class RabbitRpcClient(RabbitMQProducer producer) : IRabbitRpcClient
    {
        public async Task<string> CallAsync(string queue, string payload, CancellationToken ct = default)
        {
            var props = await producer.SendMessageAsync(queue, payload, ct);
            return await producer.GetResponseAsync(props, ct);
        }
    }
}
