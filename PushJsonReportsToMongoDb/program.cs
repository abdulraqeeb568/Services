using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace SQLiteToMongoSyncService
{
    public class Program
    {
        static void Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                x.Service<SQLiteToMongoSync>(s =>
                {
                    s.ConstructUsing(reportSummary => new SQLiteToMongoSync());
                    s.WhenStarted(reportSummary => reportSummary.Start());
                    s.WhenStopped(reportSummary => reportSummary.Stop());
                });
                x.RunAsLocalSystem();
                x.SetServiceName("SQLiteToMongoDBSyncService");
                x.SetDisplayName("SQLite To MongoDB Sync Service");
                x.SetDescription("This service synchronize SQLiteDB to MongoDB");
            });
            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}
