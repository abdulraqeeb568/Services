using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushJsonReportsToMongoDbService
{
    public class JsonReportModel
    {
        public FileInformation FileInformation { get; set; }
        public TestInformation TestInformation { get; set; }
        public List<TestCaseData> TestCaseData { get; set; }
    }
    public class JsonReportUpdatedModel
    {
        public DateTime TestDateTime { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string ExecutionStage { get; set; }

        public FileInformation FileInformation { get; set; }
        public TestInformation TestInformation { get; set; }
        public List<TestCaseData> TestCaseData { get; set; }
    }
    public class FileInformation
    {
        public string FileRevision { get; set; }
        public DateTime FileCreationDate { get; set; }

    }
    public class SoftwareInformation
    {
        public string SoftwareRevision { get; set; }
        public string FirmwareRevision { get; set; }
        public DateTime? SoftwareRevisionReleaseDate { get; set; }
        public DateTime? FirmwareRevisionReleaseDate { get; set; }

    }
    public class TestInformation
    {
        public string SerialNumber { get; set; }
        public string JabilFLSerialNumber { get; set; }
        public string TesterName { get; set; }
        public string TestName { get; set; }
        public string BoardName { get; set; }
        public string TestStatus { get; set; }
        public string AssemblyNumber { get; set; }
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public int TotalTestCasesCount { get; set; }
        public int PassedTestCasesCount { get; set; }
        public int FailedTestCasesCount { get; set; }
        public List<string> PassedTestCasesNames { get; set; }
        public List<string> FailedTestCasesNames { get; set; }
        public SoftwareInformation SoftwareInformation { get; set; }
        public string Status { get; set; }
        public string UserRole { get; set; }

    }
    public class TestSteps
    {
        public string TestDescription { get; set; }
        public string ResultValue { get; set; }
        public string ResultUnit { get; set; }
        public string Status { get; set; }
        public string CriteriaType { get; set; }
        public string CriteriaUpperLimit { get; set; }
        public string CriteriaLowerLimit { get; set; }
        public string CriteriaTolerance { get; set; }

    }
    public class TestCaseData
    {
        public string TestCaseName { get; set; }
        public string TestStatus { get; set; }
        public List<TestSteps> TestSteps { get; set; }

    }
   
}
