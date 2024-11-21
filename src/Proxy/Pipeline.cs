namespace Proxy;

public class Pipeline
{
    public Pipeline()
    {
        Steps = new List<Step>();
        Observers = new List<Observer>();
    }

    public List<Observer> Observers { get; set; }
    public List<Step> Steps { get; set; }
}