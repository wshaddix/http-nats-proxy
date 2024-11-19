namespace Proxy
{
    public class Step
    {
        public required string Direction { get; set; }
        public int Order { get; set; }
        public required string Pattern { get; set; }
        public required string Subject { get; set; }
    }
}