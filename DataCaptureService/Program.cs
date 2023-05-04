using RabbitMQ.Client;

public class DataCapture
{
    private static FileSystemWatcher _watcher;

    public static void Main()
    {
        try
        {
            StartServiceQueue();
            CreateWatcher();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
        finally
        {
            if (_watcher != null )
            {
                Console.WriteLine("Disposing file watcher");
                _watcher.Dispose();
            }
        }
    }

    private static void StartServiceQueue()
    {
        Console.WriteLine("Starting service queue");
        var factory = new ConnectionFactory { HostName = "localhost"};
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queue: "FileQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        Console.WriteLine("Service queue started");
    }

    public static void CreateWatcher()
    {
        Console.WriteLine("Creating watcher");
        _watcher = new FileSystemWatcher(@"C:\TempDirectory", "*.pdf");
        _watcher.NotifyFilter = NotifyFilters.Attributes
                                        | NotifyFilters.CreationTime
                                        | NotifyFilters.DirectoryName
                                        | NotifyFilters.FileName
                                        | NotifyFilters.LastAccess
                                        | NotifyFilters.LastWrite
                                        | NotifyFilters.Security
                                        | NotifyFilters.Size;
        _watcher.Created += OnChanged;

        _watcher.EnableRaisingEvents = true;
        Console.WriteLine("Watcher created");
    }
    
    public static void SendFile(byte[] bytes, string fileName)
    {
        Console.WriteLine("File acquired");
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var props = channel.CreateBasicProperties();
        var dict = new Dictionary<string, object>
        {
            { "fileName", fileName }
        };
        props.Headers = dict;

        channel.QueueDeclare(queue: "FileQueue",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        channel.BasicPublish(exchange: string.Empty,
                     routingKey: "FileQueue",
                     basicProperties: props,
                     body: bytes);
        Console.WriteLine("File sent");
    }

    public static void OnChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine("New file aquired");
        SendFile(File.ReadAllBytes(e.FullPath), e.Name);
    }
    
}