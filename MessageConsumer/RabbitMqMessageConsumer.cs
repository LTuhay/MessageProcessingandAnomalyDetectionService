using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MessageProcessingandAnomalyDetectionService.MessageConsumer
{
    public class RabbitMqMessageConsumer : IMessageConsumer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqMessageConsumer(string hostName, string userName, string password)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    UserName = userName,
                    Password = password
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating connection: {ex.Message}");
                throw; 
            }
        }

        public void StartConsuming(string queueName, Action<string> onMessageReceived)
        {
            try
            {
                _channel.QueueDeclare(queue: queueName,
                                      durable: true,
                                      exclusive: false,
                                      autoDelete: false,
                                      arguments: null);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        onMessageReceived(message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");
                    }
                };

                _channel.BasicConsume(queue: queueName,
                                      autoAck: true,
                                      consumer: consumer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting consumption: {ex.Message}");
                throw; 
            }
        }

        public void StopConsuming()
        {
            try
            {
                _connection.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing connection: {ex.Message}");
            }
        }
    }
}
