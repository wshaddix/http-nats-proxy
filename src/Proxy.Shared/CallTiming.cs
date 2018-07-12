namespace Proxy.Shared
{
    public class CallTiming
    {
        public long EllapsedMs { get; set; }

        public string Subject { get; set; }

        public CallTiming(string subject, long ellapsedMs)
        {
            Subject = subject;
            EllapsedMs = ellapsedMs;
        }
    }
}