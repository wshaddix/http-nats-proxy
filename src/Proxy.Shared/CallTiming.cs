namespace Proxy.Shared;

public class CallTiming(string? subject, long ellapsedMs)
{
    public long EllapsedMs { get; set; } = ellapsedMs;

    public string? Subject { get; set; } = subject;
}