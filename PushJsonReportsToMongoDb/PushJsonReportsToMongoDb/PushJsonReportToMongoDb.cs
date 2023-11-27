using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using PS19.ATM.ReturnStatus;
using PS19.SW.HDATM.Report;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace PushJsonReportsToMongoDbService
{
    public class PushJsonReportToMongoDb
    {
        private const string DATABASE_CONNECTION_STRING = "mongodb://172.19.17.20:27017";
        private const string DATABASE_NAME = "hardware_atm_database";
        private const string COLLECTION_NAME = "JsonReport";
        bool isPushingReportsComplete = true;

        private readonly Timer syncTimer;
        private const int SyncTimerInterval = 1 * 1000;//5 minutes
        private bool syncInProgress = false;
        private static readonly string[] Directories = new string[]
        {
            @"J:\Box\Fabrinet Test Data\Fabrinet.TH\JSON",
            @"J:\Box\Jabil.RTP\JSON",
            @"J:\Box\JABIL Test Data\Jabil.FL\JSON",
            @"J:\Box\JABIL Test Data\Jabil.HU\JSON",
            @"J:\Box\PS19_HWATM_Test_Data\JSON",

        };
        static MongoClient client = new MongoClient(DATABASE_CONNECTION_STRING);

        public void Start()
        {

            syncTimer.Enabled = true;
            syncTimer.Start();
        }

        public async void Stop()
        {
            syncTimer.Stop();
            //var response = await SlackNotification.SendSlackNotificationForServiceShutDown();
            //Logging.LogText($"Service Stoppedd at:response:{response.message.text.ToString()}_{DateTime.Now}", pathForLog);
        }


        public PushJsonReportToMongoDb()
        {
            syncTimer = new Timer(SyncTimerInterval) { AutoReset = true };//if true. every time timerelapsed event is fired timer interval is set to the value in parentheses
            syncTimer.Elapsed += SyncTimerElapsed;
        }
        private void SyncTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Status status = new Status();

            status = PushJsonReports();
            if (status.ErrorOccurred)
            {
                //Log
            }
        }
        public Status PushJsonReports()
        {
            Status status = new Status();
            try
            {
                if (!isPushingReportsComplete)
                {
                    status.ErrorOccurred = false;
                    status.ReturnedMessage = "Already pushing reports. Try later";
                    return status;
                }
                Stopwatch stopwatch = new Stopwatch();
                Stopwatch stopwatchAllDirectories = new Stopwatch();
                stopwatchAllDirectories.Start();
                isPushingReportsComplete = false;

                for (int j = 0; j < Directories.Length; j++)
                {
                    stopwatch = new Stopwatch();
                    stopwatch.Start();
                    string[] currentDirectories = Directories[j].Split('\\');
                    string currentDirectory = currentDirectories[currentDirectories.Length - 3];

                    string[] filePaths = Directory.GetFiles(Directories[j], "*.json", SearchOption.AllDirectories);
                    Console.WriteLine($"Time taken to get all json files for {currentDirectory}: " + stopwatch.Elapsed.ToString(@"m\:ss"));
                    stopwatch = new Stopwatch();
                    stopwatch.Start();

                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        status = ValidateJsonAndConvertToBson(filePaths[i], out JsonReportUpdatedModel jsonReportUpdatedModel);
                        if (status.ErrorOccurred)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            string error = $"Json is not valid or unable to convert to BSON for this file {Path.GetFileName(filePaths[i])}";
                            File.AppendAllText("JsonReportLogs.txt", error + " \n");
                            SlackNotification.SendSlackNotification(error);
                            continue;
                        }

                        status = CheckIfReportExists(filePaths[i], out bool ifExists);
                        if (status.ErrorOccurred)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine($"Unable to check if report:{Path.GetFileName(filePaths[i])} already exists in database ");
                            continue;
                        }
                        if (ifExists)
                        {
                            continue;
                        }

                        status = PushJsonReport(filePaths[i], jsonReportUpdatedModel);
                        if (status.ErrorOccurred)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            string error = $"Failed to push {Path.GetFileName(filePaths[i])} | Error:{status.ReturnedMessage}";
                            File.AppendAllText("JsonReportLogs.txt", error + " \n");

                            SlackNotification.SendSlackNotification(error);
                            continue;
                        }
                    }
                    Console.WriteLine($"Time taken to upload {currentDirectory} files: " + stopwatch.Elapsed.ToString(@"m\:ss"));
                }
                isPushingReportsComplete = true;
                Console.WriteLine("Time taken to upload all files: " + stopwatchAllDirectories.Elapsed.ToString(@"m\:ss"));
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = $"Error Pushing Json:{ex.Message}";
                File.AppendAllText("JsonReportLogs.txt", "Exception: " + ex.Message + " \n");
            }
            return status;
        }
        public Status ValidateJsonAndConvertToBson(string filePath, out JsonReportUpdatedModel jsonReportUpdatedModel)
        {
            Status status = new Status();
            jsonReportUpdatedModel = new JsonReportUpdatedModel();
            try
            {
                string[] executionStage = Path.GetFileName(filePath).Split('_');
                string json = File.ReadAllText(filePath).Trim();
                if (json.Trim().StartsWith("[") && json.Trim().EndsWith("]"))
                {

                    string cleanJson = json.Substring(1, json.Length - 2);
                    JsonReportModel jsonReportModel = JsonConvert.DeserializeObject<JsonReportModel>(cleanJson);

                    GetISOTestDateTime(filePath, out DateTime testDateTime);

                    jsonReportUpdatedModel.FilePath = filePath;
                    jsonReportUpdatedModel.ExecutionStage = $"Step{executionStage[1]}";
                    jsonReportUpdatedModel.FileName = Path.GetFileName(filePath);
                    jsonReportUpdatedModel.TestDateTime = testDateTime;
                    jsonReportUpdatedModel.FileInformation = jsonReportModel.FileInformation;
                    jsonReportUpdatedModel.TestInformation = jsonReportModel.TestInformation;
                    jsonReportUpdatedModel.TestCaseData = jsonReportModel.TestCaseData;

                    status.ErrorOccurred = false;
                    return status;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine($"This json didn't have square brackets are start+end {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = $"Error:{ex.Message}";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return status;
        }
        private Status GetISOTestDateTime(string filePath, out DateTime testDateTime)
        {
            Status status = new Status();
            testDateTime = new DateTime();
            try
            {
                string[] filePaths = filePath.Split(' ');
                string[] testTime = filePaths[filePaths.Length - 1].Split('_');
                string[] testDate = filePaths[filePaths.Length - 2].Split('_');
                int second = Convert.ToInt32(testTime[2].Split('.')[0]);
                int minute = Convert.ToInt32(testTime[1]);
                int hour = Convert.ToInt32(testTime[0]);
                int year = Convert.ToInt32(testDate[2]);
                int day = Convert.ToInt32(testDate[1]);
                int month = Convert.ToInt32(testDate[0]);
                testDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc); // As UTC is 5 hours behind to Pakistan's time.
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = $"Error Getting Test Date Time from filePath:{ex.Message}";
            }
            return status;
        }
        public Status CheckIfReportExists(string filePath, out bool ifEsists)
        {
            Status status = new Status();
            ifEsists = false;
            try
            {
                IMongoCollection<BsonDocument> collection = client.GetDatabase(DATABASE_NAME).GetCollection<BsonDocument>(COLLECTION_NAME);
                var filter = Builders<BsonDocument>.Filter.Eq("FileName", Path.GetFileName(filePath));
                if (collection.Find(filter).ToList().Count > 0)
                {
                    ifEsists = true;
                }
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = $"Error:{ex.Message}";

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return status;
        }
        public Status PushJsonReport(string filePath, JsonReportUpdatedModel jsonReportUpdatedModel)
        {
            Status status = new Status();
            try
            {
                var collection = client.GetDatabase(DATABASE_NAME).GetCollection<JsonReportUpdatedModel>(COLLECTION_NAME);
                collection.InsertOneAsync(jsonReportUpdatedModel);
            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = $"Error:{ex.Message}";


            }
            return status;
        }
    }
}
