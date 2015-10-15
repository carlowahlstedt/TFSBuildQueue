using Microsoft.TeamFoundation.Build.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSBuildQueue.Domain
{
    public static class Extensions
    {
        #region Export

        /// <summary>
        /// Exports the queue getting a new set of the latest builds and using the new filename.
        /// </summary>
        /// <param name="queueBuilds"></param>
        /// <param name="queueFileLocation"></param>
        public static void Export(this QueueBuilds queueBuilds, string queueFileLocation)
        {
            queueBuilds.QueueFileLocation = queueFileLocation;
            queueBuilds.Export();
        }
        /// <summary>
        /// Exports the queue getting a new set of the latest builds and using the default or last used filename.
        /// </summary>
        /// <param name="queueBuilds"></param>
        public static void Export(this QueueBuilds queueBuilds)
        {
            var results = queueBuilds.GetBuilds();
            queueBuilds.Export(results);
        }

        /// <summary>
        /// Exports the queue with the current set of builds and a new filename.
        /// </summary>
        /// <param name="queueBuilds"></param>
        /// <param name="queueFileLocation"></param>
        /// <param name="results"></param>
        public static void Export(this QueueBuilds queueBuilds, string queueFileLocation, IOrderedEnumerable<IQueuedBuild> results)
        {
            queueBuilds.QueueFileLocation = queueFileLocation;
            queueBuilds.Export(results);
        }

        /// <summary>
        /// Exports the queue with the current set of builds and the default or last used filename.
        /// </summary>
        /// <param name="queueBuilds"></param>
        /// <param name="results"></param>
        public static void Export(this QueueBuilds queueBuilds, IOrderedEnumerable<IQueuedBuild> results)
        {
            queueBuilds.WriteHeaders();
            queueBuilds.WriteBody(results);
            queueBuilds.WriteFooter(results.Count());
        }

        #endregion

        public static string GetBuildsList(this IEnumerable<TfsBuild> builds)
        {
            string returnValue = string.Empty;
            foreach (var item in builds)
            {
                returnValue += item.ToString() + Environment.NewLine;
            }
            return returnValue;
        }
    }
}
