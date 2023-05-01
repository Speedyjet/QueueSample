using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.AspNetCore.Builder;
using MainProcessingService.Helpers;

class ProcessingService
{
    private static SystemConfigurationModel? _currentConfiguration;


    public static void Main(string[] args)
    {
        try
        {

            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            _currentConfiguration = ConfigurationServiceHelpers.ConfigureMainProcessingService(app.Configuration);
            Task.Run(() => GetMessages());
            app.MapGet("/", () => "Hello World!");
            app.Run();
        }
        finally
        {
            ConfigurationServiceHelpers.Dispose();
        }
    }
    

    private static void GetMessages()
    {
        var consumer = new EventingBasicConsumer(_currentConfiguration?.CurrentChannel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {message}");
        };
        _currentConfiguration?.CurrentChannel.BasicConsume(queue: _currentConfiguration!.QueueName,
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    public static void Received(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("OnDeleted");
    }
}

