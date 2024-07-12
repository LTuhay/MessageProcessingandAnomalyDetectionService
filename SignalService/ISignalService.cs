namespace MessageProcessingandAnomalyDetectionService.SignalService
{
    public interface ISignalRService
    {
        Task StartAsync();
        Task SendAlert(string type, string message);
    }

}
