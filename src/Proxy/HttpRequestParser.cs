using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Proxy
{
    internal class HttpRequestParser
    {
        public static string ParseBody(Stream requestBody)
        {
            // if there is a body with the request then read it
            using (var reader = new StreamReader(requestBody, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static Dictionary<string, object> ParseCookies(IRequestCookieCollection requestCookies)
        {
            var cookies = new Dictionary<string, object>();

            foreach (var cookie in requestCookies)
            {
                cookies.Add(cookie.Key, string.Join(',', cookie.Value));
            }

            return cookies;
        }

        public static Dictionary<string, object> ParseHeaders(IHeaderDictionary requestHeaders)
        {
            var headers = new Dictionary<string, object>();

            foreach (var header in requestHeaders)
            {
                headers.Add(header.Key, string.Join(',', header.Value));
            }

            return headers;
        }

        public static Dictionary<string, object> ParseQueryParams(IQueryCollection requestQuery)
        {
            var queryParams = new Dictionary<string, object>();

            foreach (var param in requestQuery)
            {
                queryParams.Add(param.Key, string.Join(',', param.Value));
            }

            return queryParams;
        }
    }
}