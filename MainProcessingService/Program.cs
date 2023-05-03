using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

class ProcessingService
{

    private static IModel _connection;
    private static SystemConfigurationModel _currentConfiguration;

    public static void Main(string[] args)
    {
        
        IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();
        Console.WriteLine("Getting configuration");
        _currentConfiguration = config.GetRequiredSection("Settings").Get<SystemConfigurationModel>();
        Console.WriteLine("Creating configuration queue");
        Task.Run(() => CreateConfigurationQueue());
        Console.WriteLine("Creating queue");
        CreateQueue();
        Console.WriteLine("Subscribing to the queue");
        SubscribeToRecieveMessages();
        Console.WriteLine("Creating heartbeat queue");
        Task.Run(() => CreateHeartBeatQueue());
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }

    private static void CreateHeartBeatQueue()
    {
        var channel = GetConnection();
        channel.QueueDeclare(queue: _currentConfiguration.HeartBeatQueueName,
                     durable: false,
                     exclusive: true,
                     autoDelete: true,
                     arguments: null);
        var consumer = new EventingBasicConsumer(GetConnection());
        consumer.Received += OnHeartbeatMessageRecieved;
        GetConnection().BasicConsume(queue: _currentConfiguration.HeartBeatQueueName,
                             autoAck: true,
                             consumer: consumer);
    }

    private static void OnHeartbeatMessageRecieved(object? sender, BasicDeliverEventArgs e)
    {
        var body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine(message);
    }

    private static IModel GetConnection()
    {
        if (_connection == null)
        {
            var factory = new ConnectionFactory { HostName = _currentConfiguration.HostName };
            var connection = factory.CreateConnection();
            return connection.CreateModel();
        }
        return _connection;
    }

    private static void CreateConfigurationQueue()
    {
        var channel = GetConnection();
        var configurationBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_currentConfiguration));
        channel.ExchangeDeclare(exchange: _currentConfiguration.ConfigurationQueueName,
                     type: ExchangeType.Fanout);
        channel.BasicPublish(exchange: _currentConfiguration.ConfigurationQueueName,
            routingKey: string.Empty,
            basicProperties: null,
            body: configurationBody);
    }

    private static void CreateQueue()
    {
        var channel = GetConnection();
        channel.QueueDeclare(queue: _currentConfiguration.QueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
    }

    private static void SubscribeToRecieveMessages()
    {
        var consumer = new EventingBasicConsumer(GetConnection());
        consumer.Received += OnMessageRecieved;
        GetConnection().BasicConsume(queue: _currentConfiguration.QueueName,
                             autoAck: true,
                             consumer: consumer);
    }

    private static void OnMessageRecieved(object? sender, BasicDeliverEventArgs e)
    {
        var fileBytes = e.Body;
        Console.WriteLine($" File received");
        var tempDirectory = Path.GetTempPath();
        var tempFileName = Path.GetRandomFileName();
        File.WriteAllBytes(Path.Combine(tempDirectory, tempFileName), fileBytes.ToArray());
    }
}

