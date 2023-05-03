using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

class ProcessingService
{
    public static void Main(string[] args)
    {
        
        IConfiguration config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddEnvironmentVariables()
        .Build();
        Console.WriteLine("Getting configuration");
        var currentConfiguration = config.GetRequiredSection("Settings").Get<SystemConfigurationModel>();
        Console.WriteLine("Creating configuration queue");
        CreateConfigurationQueue(currentConfiguration);
        Console.WriteLine("Creating queue");
        var queue = CreateQueue(currentConfiguration.QueueName);
        Console.WriteLine("Subscribing to the queue");
        GetMessages(queue, currentConfiguration.QueueName);
        Console.ReadKey();
    }

    private static void CreateConfigurationQueue(SystemConfigurationModel? currentConfiguration)
    {
        var factory = new ConnectionFactory { HostName = currentConfiguration.HostName };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        var configurationBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(currentConfiguration));
        channel.ExchangeDeclare(exchange: currentConfiguration.ConfigurationQueueName,
                     type: ExchangeType.Fanout);
        channel.BasicPublish(exchange: currentConfiguration.ConfigurationQueueName,
            routingKey: string.Empty,
            basicProperties: null,
            body: configurationBody);
    }

    private static IModel CreateQueue(string queueName)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        channel.QueueDeclare(queue: queueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        return channel;
    }

    private static void GetMessages(IModel channelModel, string queueName)
    {
        var consumer = new EventingBasicConsumer(channelModel);
        consumer.Received += OnMessageRecieved;
        //    (model, ea) =>
        //{
        //    var body = ea.Body.ToArray();
        //    var message = Encoding.UTF8.GetString(body);
        //    Console.WriteLine($" [x] Received {message}");
        //};
        channelModel.BasicConsume(queue: queueName,
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    private static void OnMessageRecieved(object? sender, BasicDeliverEventArgs e)
    {
        var body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($" [x] Received {message}");
    }

    public static void Received(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("OnDeleted");
    }
}

