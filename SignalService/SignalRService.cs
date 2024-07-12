using Microsoft.AspNetCore.SignalR.Client;

namespace MessageProcessingandAnomalyDetectionService.SignalService
{
    public class SignalRService : ISignalRService
    {
        private readonly string _signalRUrl;
        private HubConnection _connection;

        public SignalRService(string signalRUrl)
        {
            _signalRUrl = signalRUrl;
            _connection = new HubConnectionBuilder()
                .WithUrl(_signalRUrl)
                .Build();
        }

        public async Task StartAsync()
        {
            try
            {
                await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting SignalR connection: {ex.Message}");
            }
        }

        public async Task SendAlert(string type, string message)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await _connection.InvokeAsync("SendAlert", type, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending alert: {ex.Message}");
                }
            }
        }
    }
}
