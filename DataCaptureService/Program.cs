//Implement Data capture service which will listen to a specific local folder
//and retrieve documents of some specific format (i.e., PDF) and send to Main processing service through message queue. 

using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class DataCapture
{
    private static IModel? _channel;
    private static ServiceConfiguration _serviceConfiguration;

    public static void Main()
    {
        GetInitailConfiguration("ConfigurationQueue");
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

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        } 
    }

    public static void SendMessage(string message)
    {
        Console.WriteLine(message);
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        _channel = channel;
        channel.QueueDeclare(queue: "TestQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: "TestQueue",
                     basicProperties: null,
                     body: body);
    }

    public static void OnChanged(object sender, FileSystemEventArgs e)
    {
        SendMessage("OnChanged");
    }
    
}