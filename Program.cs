using MessageProcessingandAnomalyDetectionService.Config;
using MessageProcessingandAnomalyDetectionService.MessageConsumer;
using MessageProcessingandAnomalyDetectionService.MessagesProcessor;
using MessageProcessingandAnomalyDetectionService.ServerStats;
using MessageProcessingandAnomalyDetectionService.SignalService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace ServerMonitoringAndNotificationSystem
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    var rabbitMqConfig = context.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
                    var mongoConfig = context.Configuration.GetSection("MongoDB").Get<MongoDbConfig>();

                    services.AddSingleton(rabbitMqConfig);
                    services.AddSingleton<IMessageConsumer, RabbitMqMessageConsumer>(sp =>
                        new RabbitMqMessageConsumer(rabbitMqConfig.HostName, rabbitMqConfig.UserName, rabbitMqConfig.Password));

                    var mongoClient = new MongoClient(mongoConfig.ConnectionString);
                    var database = mongoClient.GetDatabase(mongoConfig.DatabaseName);
                    services.AddSingleton(database);

                    var signalRConfig = context.Configuration.GetSection("SignalRConfig");
                    services.AddSingleton<ISignalRService, SignalRService>(sp =>
                        new SignalRService(signalRConfig.GetValue<string>("SignalRUrl")));

                    services.AddSingleton<IMessageProcessor, MessageProcessor>();
                });

            var host = builder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var messageConsumer = services.GetRequiredService<IMessageConsumer>();
                var messageProcessor = services.GetRequiredService<IMessageProcessor>();
                var signalRService = services.GetRequiredService<ISignalRService>();

                await signalRService.StartAsync();

                messageConsumer.StartConsuming("ServerStatisticsQueue", async (message) =>
                {
                    var statistics = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerStatistics>(message);
                    await messageProcessor.ProcessMessage(statistics);
                });

                await host.RunAsync();
            }
        }
    }
}
