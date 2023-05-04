using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using System.Threading.Channels;
using System.Text;

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
        Console.WriteLine("Creating queue");
        var connection = CreateQueue(currentConfiguration);
        Console.WriteLine("Subscribing to the queue");
        SubscribeToRecieveMessages(connection, currentConfiguration);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }

    private static IModel CreateQueue(SystemConfigurationModel? currentConfiguration)
    {
        var factory = new ConnectionFactory { HostName = currentConfiguration.HostName };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        channel.QueueDeclare(queue: currentConfiguration.QueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        return channel;
    }

    private static void SubscribeToRecieveMessages(IModel connection, SystemConfigurationModel config)
    {
        var consumer = new EventingBasicConsumer(connection);
        consumer.Received += OnMessageRecieved;
        connection.BasicConsume(queue: config.QueueName,
                             autoAck: true,
                             consumer: consumer);
    }

    private static void OnMessageRecieved(object? sender, BasicDeliverEventArgs e)
    {
        byte[] nameBytes = (byte[])e.BasicProperties.Headers["fileName"];
        var name = Encoding.UTF8.GetString(nameBytes);
        var fileBytes = e.Body;
        var tempDirectory = Path.GetTempPath();
        Console.WriteLine($"The file with name {name} will be stored in {tempDirectory} directory");
        File.WriteAllBytes(Path.Combine(tempDirectory, name), fileBytes.ToArray());
        //TODO add extension or pass the file name on key header. Show the path to file
    }
}

