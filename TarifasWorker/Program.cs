using KafkaFlow;
using KafkaFlow.Serializer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var bootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var tarifaValor = decimal.Parse(builder.Configuration["Tarifas:Valor"] ?? "2.00");

builder.Services.AddKafka(kafka =>
    kafka.UseConsoleLog()
        .AddCluster(cluster =>
            cluster.WithBrokers(new[] { bootstrap })
                .CreateTopicIfNotExists("transfers.completed", 1, 1)
                .CreateTopicIfNotExists("tarifas.completed", 1, 1)
                .AddConsumer(consumer => consumer
                    .Topic("transfers.completed")
                    .WithGroupId("tarifas-worker")
                    .WithBufferSize(10)
                    .WithWorkersCount(1)
                    .AddMiddlewares(m => m
                        .AddSerializer<KafkaFlow.Serializer.NewtonsoftJson.NewtonsoftJsonSerializer>()
                        .Add<TransferConsumerMiddleware>(sp => new TransferConsumerMiddleware(tarifaValor))
                        .AddSerializer<KafkaFlow.Serializer.NewtonsoftJson.NewtonsoftJsonSerializer>()))
                .AddProducer("tarifas-producer", producer => producer
                    .DefaultTopic("tarifas.completed")
                    .AddMiddlewares(m => m.AddSerializer<KafkaFlow.Serializer.NewtonsoftJson.NewtonsoftJsonSerializer>()))
        )
);

builder.Services.AddHostedService<KafkaBusHostedService>();

var host = builder.Build();
await host.RunAsync();

public sealed record TransferDone(string RequestId, string AccountId);
public sealed record TarifaDone(string AccountId, decimal Value);

public class TransferConsumerMiddleware : IMessageMiddleware
{
    private readonly decimal _tarifa;
    public TransferConsumerMiddleware(decimal tarifa) => _tarifa = tarifa;

    public async Task Invoke(IMessageContext context, MiddlewareDelegate next)
    {
        var message = context.Message.Value as TransferDone;
        if (message is not null)
        {
            var producer = context.ServiceProvider.GetRequiredService<IProducerAccessor>().GetProducer("tarifas-producer");
            await producer.ProduceAsync(context.Message.Key, new TarifaDone(message.AccountId, _tarifa));
        }
        await next(context);
    }
}

public class KafkaBusHostedService : IHostedService
{
    private readonly IKafkaBus _bus;
    public KafkaBusHostedService(IKafkaBus bus) => _bus = bus;

    public Task StartAsync(CancellationToken cancellationToken) => _bus.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => _bus.StopAsync(cancellationToken);
}
