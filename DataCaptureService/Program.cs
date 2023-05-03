using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class DataCapture
{
    private static ServiceConfiguration _serviceConfiguration;
    private static bool isWorking;

    public static void Main()
    {
        Task.Run(() => GetInitailConfiguration("ConfigurationQueue"));
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }

    private static void GetInitailConfiguration(string configurationChannelName)
    {
        var configuration = new ServiceConfiguration();
        var factory = new ConnectionFactory { HostName = "localhost" };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare(exchange: configurationChannelName, type: ExchangeType.Fanout);

        var queueName = channel.QueueDeclare().QueueName;
        channel.QueueBind(queue: queueName,
                          exchange: configurationChannelName,
                          routingKey: string.Empty);

        Console.WriteLine(" [*] Waiting for configuration.");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += OnConfigurationRecieved;

        channel.BasicConsume(queue: queueName,
                             autoAck: true,
                             consumer: consumer);
    }

    public static void OnConfigurationRecieved(object sender, BasicDeliverEventArgs e)
    {
        byte[] body = e.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine("New configuration acquired");
        _serviceConfiguration = JsonSerializer.Deserialize<ServiceConfiguration>(message);
        CreateWatcher();
        Task.Run(() =>
        {
            StartHeartBeatService();
        });
    }

    private static void StartHeartBeatService()
    {
        Console.WriteLine("Starting heartbeat service");
        while (true)
        {
            Task.Delay(6000).GetAwaiter().GetResult();
            SendHeartbeat();
        }
    }

    private static void SendHeartbeat()
    {
        Console.WriteLine("Sending heartbeat");
        var factory = new ConnectionFactory { HostName = _serviceConfiguration.HostName };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: _serviceConfiguration.HeartBeatQueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        var message = isWorking ? "Working" : "Idle";
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: _serviceConfiguration.HeartBeatQueueName,
                     basicProperties: null,
                     body: body);
    }

    public static void CreateWatcher()
    {
        Console.WriteLine("Creating watcher");
        using (FileSystemWatcher watcher = new FileSystemWatcher(_serviceConfiguration.LocalDirectory))
        {
            watcher.NotifyFilter = NotifyFilters.Attributes
                                         | NotifyFilters.CreationTime
                                         | NotifyFilters.DirectoryName
                                         | NotifyFilters.FileName
                                         | NotifyFilters.LastAccess
                                         | NotifyFilters.LastWrite
                                         | NotifyFilters.Security
                                         | NotifyFilters.Size;
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Renamed += OnChanged;

            watcher.Filter = "*.pdf";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            Console.WriteLine("Watcher created");
        } 
    }

    public static void SendFile(byte[] bytes)
    {
        Console.WriteLine("File acquired");
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: _serviceConfiguration.QueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: _serviceConfiguration.QueueName,
                     basicProperties: null,
                     body: bytes);
        Console.WriteLine("File sent");
    }

    public static void OnChanged(object sender, FileSystemEventArgs e)
    {
        isWorking = true;
        SendFile(File.ReadAllBytes(e.FullPath));
    }
    
}