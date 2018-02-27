using System.Collections.Generic;

namespace Proxy
{
    public class Pipeline
    {
        public List<Observer> Observers { get; set; }
        public List<Step> Steps { get; set; }

        public Pipeline()
        {
            Steps = new List<Step>();
            Observers = new List<Observer>();
        }
    }
}