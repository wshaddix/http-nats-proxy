using System.Collections.Generic;

namespace Proxy
{
    public class Pipeline
    {
        public List<Step> Steps { get; set; }

        public Pipeline()
        {
            Steps = new List<Step>();
        }
    }
}