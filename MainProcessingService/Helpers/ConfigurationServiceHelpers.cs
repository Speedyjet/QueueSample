using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Threading.Channels;

namespace MainProcessingService.Helpers
{
    public class ConfigurationServiceHelpers
    {

        private static IConnection? _currentConnection;
        private static IConfiguration? _configuration;
        private static SystemConfigurationModel? _currentConfiguration;
        private static IModel _currentChannel;

        public static SystemConfigurationModel ConfigureMainProcessingService(IConfiguration configuration)
        {
            _configuration = configuration;
            var config = GetConfiguration(_configuration);
            CreateQueue(config.QueueName);
            return config;
        }

        private static void CreateQueue(string queueName)
        {
            _currentChannel.QueueDeclare(queue: queueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        }

        private static SystemConfigurationModel GetConfiguration(IConfiguration configuration)
        {
            return new SystemConfigurationModel()
            {
                QueueName = configuration.GetValue<string>("QueueName"),
                LocalDirectory = configuration["LocalDirectory"],
                CurrentChannel = GetCurrentChannel(),
            };
        }

        private static IModel GetCurrentChannel()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            var connection = factory.CreateConnection();
            _currentChannel = connection.CreateModel();
            return _currentChannel;
        }

        public static void Dispose()
        {
            if (_currentConnection != null)
            {
                _currentConnection.Close();
                _currentConnection.Dispose();
            }
            if (_currentChannel != null)
            {
                _currentChannel.QueueDeleteNoWait(_currentConfiguration!.QueueName);
                _currentChannel.Close();
                _currentChannel.Dispose();
            }
        }
    }
}
