using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TFSBuildQueue.Domain
{
    public class QueueBuilds : IDisposable
    {
        public string TFS_URL = "http://tfs.com:8080/tfs/DefaultCollection";

        public static string FileName = "TFSBuilds.csv";
        public string QueueFileLocation = string.Format("{0}\\{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), FileName);

        public IQueuedBuildQueryResult BuildQueryResult;

        private TfsTeamProjectCollection _TFS;
        private IBuildServer _BuildServer;
        private IQueuedBuildSpec _QueuedBuildSpec;

        public QueueBuilds(string tfsUrl)
        {
            TFS_URL = tfsUrl;
        }

        public async Task Setup()
        {
            await Task.Run(() => _TFS = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(TFS_URL)));
            await Task.Run(() => _BuildServer = _TFS.GetService<IBuildServer>());
            await Task.Run(() => _QueuedBuildSpec = _BuildServer.CreateBuildQueueSpec("*", "*"));
        }

        public IOrderedEnumerable<IQueuedBuild> GetBuilds()
        {
            BuildQueryResult = _BuildServer.QueryQueuedBuilds(_QueuedBuildSpec);
            return from builds in BuildQueryResult.QueuedBuilds
                   orderby builds.BuildController ascending, builds.QueueTime ascending
                   select builds;
        }

        #region BuildAgent

        public static string GetBuildAgent(IBuildDetail build)
        {
            if (build != null)
            {
                build.RefreshAllDetails();
                foreach (var child in build.Information.Nodes)
                {
                    string AgentName = ShowChild(child, 1);
                    if (!string.IsNullOrEmpty(AgentName))
                    {
                        return AgentName;
                    }
                }
            }
            return string.Empty;
        }

        static string ShowChild(IBuildInformationNode node, int level)
        {
            string levelStr = new string(' ', level * 4);
            foreach (var field in node.Fields)
            {
                if (field.Key == "ReservedAgentName")
                {
                    return field.Value;
                }
            }

            foreach (var child in node.Children.Nodes)
            {
                string AgentName = ShowChild(child, level + 1);
                if (!string.IsNullOrEmpty(AgentName))
                {
                    return AgentName;
                }
            }
            return string.Empty;
        }

        #endregion

        #region CSV

        public void WriteHeaders()
        {
            Console.WriteLine("Writing Headers");
            using (var writeStream = new StreamWriter(QueueFileLocation, false))
            {
                writeStream.WriteLine("Build Controller,Agent,Project,Build Definition,Status,Priority,Date & Time Started,Elapsed Time,User");
            }
        }

        public void WriteBody(IOrderedEnumerable<IQueuedBuild> results)
        {
            Console.WriteLine("Writing Body");
            foreach (var build in results)
            {
                Console.WriteLine(string.Format("Writing Build:{0}{1}", build.BuildDefinition.Name, Environment.NewLine));
                using (var writeStream = new StreamWriter(QueueFileLocation, true))
                {
                    writeStream.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                        build.BuildController.Name,
                        GetBuildAgent(build.Build),
                        build.TeamProject,
                        build.BuildDefinition.Name,
                        build.Status,
                        build.Priority,
                        build.QueueTime,
                        (DateTime.Now - build.QueueTime),
                        build.RequestedForDisplayName);
                }
            }
        }

        public void WriteFooter(int buildCount)
        {
            Console.WriteLine("Writing Footer");
            using (var writeStream = new StreamWriter(QueueFileLocation, true))
            {
                writeStream.WriteLine("\n\nTotal Builds Queued: " + buildCount);
            }
            Console.WriteLine("\n\nTotal Builds Queued: " + buildCount + "\n\n");
        }

        #endregion

        public void Dispose()
        {
            _TFS.Dispose();
        }
    }
}
