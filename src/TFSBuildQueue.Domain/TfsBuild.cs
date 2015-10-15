using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSBuildQueue.Domain
{
    public class TfsBuild
    {
        public string BuildControllerName { get; set; }
        public string Agent { get; set; }
        public string TeamProject { get; set; }
        public string BuildDefinitionName { get; set; }
        public string Status { get; set; }
        public QueuePriority Priority { get; set; }
        public DateTime QueueTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public string Reason { get; set; }
        public string Requestor { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", BuildDefinitionName, Agent);
        }
    }
}
