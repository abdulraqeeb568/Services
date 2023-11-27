using MongoDB.Driver;
using PS19.ATM.ReturnStatus;
using System;

namespace LocalMongoDatabase
{
    public class LocalMongoDatabase
    {
        public Status ConnectDatabase()
        {
            MongoClient mongoClient = new MongoClient(connectionString);
            boardCollection = mongoClient.GetDatabase(dbName).GetCollection<Board>("Board");
        }
        public Status CheckIfDocumentExists()
        {
            Status status = new Status();
           
            try
            {

            }
            catch (Exception ex)
            {
                status.ErrorOccurred = true;
                status.ReturnedMessage = ex.Message;
            }
            return status;
        }
    }
}
