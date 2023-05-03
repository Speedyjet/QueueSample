using RabbitMQ.Client;

public class SystemConfigurationModel
{
    public string LocalDirectory { get; set; }
    public string QueueName { get; set; } = "TestQueue";
    public IModel CurrentChannel { get; set; }
    public string ConfigurationQueueName {get;set; }
    public string HostName { get; set; }
}