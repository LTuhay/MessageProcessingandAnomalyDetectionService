namespace MessageProcessingandAnomalyDetectionService.MessageConsumer
{
    public interface IMessageConsumer
    {
        void StartConsuming(string queueName, Action<string> onMessageReceived);
        void StopConsuming();
    }
}
