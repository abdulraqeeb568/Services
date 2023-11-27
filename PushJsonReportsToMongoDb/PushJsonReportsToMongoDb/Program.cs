using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using PS19.ATM.ReturnStatus;
using PushJsonReportsToMongoDbService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace PushJsonReportsToMongoDbService
{
    public class Program
    {
        //TODO: Take folder path from App.Config
  


        static async Task Main(string[] args)
        {
            var exitCode = HostFactory.Run(x =>
            {
                x.Service<PushJsonReportToMongoDb>(s =>
                {
                    s.ConstructUsing(pushJsonReport => new PushJsonReportToMongoDb());
                    s.WhenStarted(pushJsonReport => pushJsonReport.Start());
                    s.WhenStopped(pushJsonReport => pushJsonReport.Stop());
                });
                x.RunAsLocalSystem();
                x.SetServiceName("PushJsonReportsToMongoService");
                x.SetDisplayName("Push Json Reports To MongoService");
                x.SetDescription("This service synchronize SQLiteDB to MongoDB");
            });
            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;

        }


    }
}
