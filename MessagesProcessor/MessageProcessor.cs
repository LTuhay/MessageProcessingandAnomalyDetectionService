using MessageProcessingandAnomalyDetectionService.ServerStats;
using MessageProcessingandAnomalyDetectionService.SignalService;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;


namespace MessageProcessingandAnomalyDetectionService.MessagesProcessor
{
    public class MessageProcessor : IMessageProcessor
    {
        private readonly IMongoCollection<ServerStatistics> _collection;
        private readonly IConfiguration _configuration;
        private readonly ISignalRService _signalRService;

        public MessageProcessor(IMongoDatabase database, IConfiguration configuration, ISignalRService signalRService)
        {
            _collection = database.GetCollection<ServerStatistics>("ServerStatistics");
            _configuration = configuration;
            _signalRService = signalRService;
        }

        public async Task ProcessMessage(ServerStatistics statistics)
        {
            try
            {
                await _collection.InsertOneAsync(statistics);
                DetectAndSendAlerts(statistics);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }
        }

        private void DetectAndSendAlerts(ServerStatistics statistics)
        {
            try
            {
                var anomalyConfig = _configuration.GetSection("AnomalyDetectionConfig");
                var memoryAnomalyThreshold = anomalyConfig.GetValue<double>("MemoryUsageAnomalyThresholdPercentage");
                var cpuAnomalyThreshold = anomalyConfig.GetValue<double>("CpuUsageAnomalyThresholdPercentage");
                var memoryUsageThreshold = anomalyConfig.GetValue<double>("MemoryUsageThresholdPercentage");
                var cpuUsageThreshold = anomalyConfig.GetValue<double>("CpuUsageThresholdPercentage");

                if (statistics.MemoryUsage > statistics.AvailableMemory * (1 + memoryAnomalyThreshold))
                {
                    SendSignalRAlert("Anomaly Alert", $"Memory Usage anomaly detected on server {statistics.ServerIdentifier}");
                }

                if (statistics.CpuUsage > cpuAnomalyThreshold)
                {
                    SendSignalRAlert("Anomaly Alert", $"CPU Usage anomaly detected on server {statistics.ServerIdentifier}");
                }

                var totalMemory = statistics.MemoryUsage + statistics.AvailableMemory;
                if (statistics.MemoryUsage / totalMemory > memoryUsageThreshold)
                {
                    SendSignalRAlert("High Usage Alert", $"High Memory Usage detected on server {statistics.ServerIdentifier}");
                }

                if (statistics.CpuUsage > cpuUsageThreshold)
                {
                    SendSignalRAlert("High Usage Alert", $"High CPU Usage detected on server {statistics.ServerIdentifier}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detecting alerts: {ex.Message}");
            }
        }

        private async void SendSignalRAlert(string type, string message)
        {
            try
            {
                await _signalRService.SendAlert(type, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending SignalR alert: {ex.Message}");
            }
        }
    }
}
