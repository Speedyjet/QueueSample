//Implement Data capture service which will listen to a specific local folder
//and retrieve documents of some specific format (i.e., PDF) and send to Main processing service through message queue. 

using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;

public class DataCapture
{
    private static IModel? _channel;

    public static void Main()
    {
        while (true)
        {
            CreateWatcher();
        }
    }

    public static void CreateWatcher()
    {
        using (FileSystemWatcher watcher = new FileSystemWatcher(@"c:\TempDirectory"))
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
            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.Filter = "*.pdf";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
        } 
    }

    public static void SendMessage(string message)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        _channel = channel;
        channel.QueueDeclare(queue: "FileQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: "hello",
                     basicProperties: null,
                     body: body);
    }

    public static void OnChanged(object sender, FileSystemEventArgs e)
    {
        SendMessage("OnChanged");
    }
    public static void OnCreated(object sender, FileSystemEventArgs e)
    {
        const string message = "Hello World!";
        var body = Encoding.UTF8.GetBytes(message);
        _channel?.BasicPublish(exchange: string.Empty,
                     routingKey: "hello",
                     basicProperties: null,
                     body: body);
    }
    public static void OnDeleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("OnDeleted");
    }
    public static void OnRenamed(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("OnRenamed");
    }
    public static void OnError(object sender, ErrorEventArgs e)
    {
        Console.WriteLine("OnError");
    }
}