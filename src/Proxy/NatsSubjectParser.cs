using Serilog;

namespace Proxy
{
    internal static class NatsSubjectParser
    {
        internal static string? Parse(string httpMethod, string urlPath)
        {
            // replace all forward slashes with periods in the http request path
            var subjectPath = urlPath.Replace('/', '.').TrimEnd('.');

            // the subject is the http method followed by the path all lowercased
            var subject = string.Concat(httpMethod, subjectPath).ToLower();

            // emit a log message
            Log.Information("Parsed the http request {HttpMethod} {UrlPath} to {Subject}",
                httpMethod, urlPath, subject);

            return subject;
        }
    }
}