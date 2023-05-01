//Create new queue on startup for receiving results from Data capture services. 

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using RabbitMQ.Client;

public class SystemConfigurationModel
{
    public string LocalDirectory { get; set; }
    public string QueueName { get; set; } = "TestQueue";
    public IModel CurrentChannel { get; set; }
}