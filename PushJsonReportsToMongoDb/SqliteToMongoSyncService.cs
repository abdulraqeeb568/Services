using PS19.ATM.ReturnStatus;
using System;
using System.Collections.Generic;
using System.IO;
using Timer = System.Timers.Timer;
using PS19.HDATM.DB.SQLite;
using PS19.HDATM.DB.MongoDB;
using PS19.HDATM.Commons.Models;
using PS19.HDATM.Commons.Models.SQLite;
using PS19.HDATM.Commons.Models.Shared;
using PS19.SW.HDATM.Report;
using System.Diagnostics;
using System.Linq;

namespace SQLiteToMongoSyncService
{
    public class SQLiteToMongoSync
    {
        private readonly Timer syncTimer;
        private string pathForLog = @"SqliteToMongoSyncLog.txt";
        private const int SyncTimerInterval = 15 * 1000;
        private bool syncInProgress = false;
        public SQLiteToMongoSync()
        {
            Status status = new Status();

            syncTimer = new Timer(SyncTimerInterval) { AutoReset = true };//if true. every time timerelapsed event is fired timer interval is set to the value in parentheses
            syncTimer.Elapsed += SyncTimerElapsed;
        }

        public void Start()
        {

            syncTimer.Enabled = true;
            syncTimer.Start();
            Logging.LogText($"Service Started at:{DateTime.Now}", pathForLog);
        }

        public async void Stop()
        {
            syncTimer.Stop();
            //var response = await SlackNotification.SendSlackNotificationForServiceShutDown();
            //Logging.LogText($"Service Stoppedd at:response:{response.message.text.ToString()}_{DateTime.Now}", pathForLog);
            Logging.LogText($"Service Stopped at:{DateTime.Now}", pathForLog);
        }

        private void SyncTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Status status = new Status();
            try
            {
                status = SyncReports();
                if (status.ErrorOccurred)
                {
                    Logging.LogStatus(status, pathForLog);
                }
            }
            catch (Exception exception)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = exception.Message;
                Logging.LogStatus(status, pathForLog);
            }
        }

        public Status SyncReports()
        {
            Status status = new Status();
            if (syncInProgress)
            {
                status.ReturnedMessage = "Syncing in process...";
                return status;
            }
            try
            {
                SQLiteDatabase sqliteDatabase = new SQLiteDatabase();
                MongoDatabase mongoDatabase = new MongoDatabase();

                List<TestCase> testCases = new List<TestCase>();
                List<TestStep> testSteps = new List<TestStep>();

                List<TestCase> newTestCases = new List<TestCase>();
                List<TestStep> newTestSteps = new List<TestStep>();
                status = sqliteDatabase.GetReports(out List<Report> reports);
                if (status.ErrorOccurred)
                {
                    return status;
                }
                syncInProgress = true;
                List<Report> reportsToBeSync = reports.FindAll(x => x.Sync == false);
                if (reportsToBeSync.Count > 0)
                {
                    status = mongoDatabase.ConnectDatabase();
                    if (status.ErrorOccurred)
                    {
                        Logging.LogStatus(status, pathForLog);
                        return status;
                    }

                    status = sqliteDatabase.GetTestCases(out  testCases);
                    status = sqliteDatabase.GetTestSteps(out testSteps);
                    status = mongoDatabase.GetTestCaseCount(out int mongoTestCaseCount);
                    status = mongoDatabase.GetTestStepCount(out int mongoTestStepCount);
                    
                    if (testCases.Count > mongoTestCaseCount)
                    {
                        newTestCases = testCases.Skip(Convert.ToInt32(mongoTestCaseCount)).ToList();
                        status = mongoDatabase.InsertManyTestCase(newTestCases);
                        if (status.ErrorOccurred)
                        {
                            syncInProgress = false;
                            status.ReturnedMessage = "New TestCases could not be inserted. Failed to seed testcases";
                            Logging.LogStatus(status, pathForLog);
                            return status;
                        }
                    }
                    if (testSteps.Count > mongoTestStepCount)
                    {
                        newTestSteps = testSteps.Skip(mongoTestStepCount).ToList();
                        status = mongoDatabase.InsertManyTestStep(newTestSteps);

                        if (status.ErrorOccurred)
                        {
                            syncInProgress = false;
                            status.ReturnedMessage = "New TestSteps could not be inserted. Failed to seed teststeps";
                            Logging.LogStatus(status, pathForLog);
                            return status;
                        }
                    }

                    if (status.ErrorOccurred)
                    {
                        Logging.LogStatus(status, pathForLog);
                        return status;
                    }
                }
                for (int i = 0; i < reportsToBeSync.Count; i++)
                {
                    status = sqliteDatabase.GetReportData(reportsToBeSync[i], out ReportData dataFromSqlite);
                    if (status.ErrorOccurred)
                    {
                        syncInProgress = false;
                        status.ReturnedMessage = "New Tests could not be inserted. Failed to seed teststeps";
                        Logging.LogStatus(status, pathForLog);
                        return status;
                    }

                    status = mongoDatabase.SyncReport(reportsToBeSync[i], dataFromSqlite);
                    //Logging.LogStatus(status, pathForLog);
                    //Logging.LogText("Running", pathForLog);
                    if (status.ErrorOccurred)
                    {
                        syncInProgress = false;
                        status.ReturnedMessage = "Report Could not be synced. Failed to insert report data";
                        Logging.LogStatus(status, pathForLog);
                        return status;
                    }
                }
                syncInProgress = false;
            }
            catch (Exception exception)
            {
                syncInProgress = false;
                status.ErrorOccurred = true;
                status.ReturnedMessage = $"Exception:{exception.Message}";
                return status;
            }
            return status;
        }



    }
}
