﻿namespace Proxy;

public class StepException : Exception
{
    public StepException(string message) : base(message)
    {
    }

    public StepException(string? subject, string pattern, string msg)
    {
        Subject = subject;
        Pattern = pattern;
        Msg = msg;
    }

    public string? Msg { get; private set; }
    public string? Pattern { get; private set; }
    public string? Subject { get; private set; }
}