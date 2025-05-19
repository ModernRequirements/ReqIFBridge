using MongoDB.Driver;
using System.Configuration;

public class MongoDBService
{
    private readonly IMongoDatabase _database;

    public MongoDBService()
    {
        var connectionString = ConfigurationManager.AppSettings["MongoDB:ConnectionString"];
        var databaseName = ConfigurationManager.AppSettings["MongoDB:DatabaseName"];
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        return _database.GetCollection<T>(collectionName);
    }
}
