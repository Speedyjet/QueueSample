using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;

class ProcessingService
{
    private static Configuration? _configuration;
    private static IConnection? _currentConnection;
    private static IModel? _channel;

    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
        await ConfigureMainProcessingService();
        CreateNewQueue();
        GetMessages();
        Dispose();
    }
    public static async Task ConfigureMainProcessingService()
    {
        if (_configuration == null)
        {
            _configuration = await GetConfiguration();
        }

        //_configuration = 
        //await CreateNewQueue(_configuration);

        //await Console.Out.WriteLineAsync("new queue started");
        //await Console.Out.WriteLineAsync("listening to the queue");
        //await Console.Out.WriteLineAsync("Press Q to exit");
        //var input = Console.ReadKey();
        //while (!string.IsNullOrEmpty(input.ToString()) 
        //    && input.Key.ToString().ToUpperInvariant() == "Q")
        //{

        //}

    }

    private static void GetMessages()
    {
        //_channel.QueueDeclare(queue: queueName,
        //                     durable: false,
        //                     exclusive: false,
        //                     autoDelete: false,
        //                     arguments: null);

        //Console.WriteLine(" [*] Waiting for messages.");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {message}");
        };
        _channel.BasicConsume(queue: _configuration!.QueueName,
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    public static void Received(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("OnDeleted");
    }

    private static Task<Configuration> GetConfiguration()
    {
        throw new NotImplementedException();
    }

    private static void CreateNewQueue()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _configuration!.QueueName,
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
    }

    private static void CreateNewConnection()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _currentConnection = factory.CreateConnection();
        _channel = _currentConnection.CreateModel();
    }

    private static void Dispose()
    {
        if (_currentConnection != null)
        {
            _currentConnection.Close();
            _currentConnection.Dispose();
        }
        if (_channel != null)
        {
            _channel.QueueDeleteNoWait(_configuration!.QueueName);
            _channel.Close();
            _channel.Dispose();
        }
    }
}

