using MessageProcessingandAnomalyDetectionService.ServerStats;

namespace MessageProcessingandAnomalyDetectionService.MessagesProcessor
{
    public interface IMessageProcessor
    {
        Task ProcessMessage(ServerStatistics statistics);

    }
}
