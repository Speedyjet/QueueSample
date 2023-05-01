//Create new queue on startup for receiving results from Data capture services. 

internal class Configuration
{
    public string LocalDirectory { get; set; }
    public string QueueName { get; set; } = "TestQueue";
}