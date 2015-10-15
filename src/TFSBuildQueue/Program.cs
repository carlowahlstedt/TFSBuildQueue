using System;
using System.Diagnostics;
using TFSBuildQueue.Domain;

namespace TFSBuildQueues
{
    class Program
    {
        static void Main(string[] args)
        {
            var builds = new QueueBuilds("http://tfs.com:8080/tfs/DefaultCollection");

            Console.WriteLine("\nTFS Build Queue");
            Console.WriteLine("===============\n");
            Console.WriteLine("Connecting to: " + builds.TFS_URL);

            builds.Export();

            Process.Start(builds.QueueFileLocation);
        }


    }
}
