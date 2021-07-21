using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Data
{
    public interface IMongoRepository
    {
        IMongoCollection<BsonDocument> UsersBson { get; }
        IMongoCollection<BsonDocument> OldHistoryBson { get; }
    }

    public class MongoRepository : IMongoRepository
    {
        public IMongoCollection<BsonDocument> UsersBson { get; }
        public IMongoCollection<BsonDocument> OldHistoryBson { get; }

        public MongoRepository(IMongoClient client, PlantsDbSettings settings)
        {
            var db = client.GetDatabase(settings.DatabaseName);
            UsersBson = db.GetCollection<BsonDocument>(settings.UsersCollectionName);
            OldHistoryBson = db.GetCollection<BsonDocument>(settings.OldHistoryCollectionName);
        }
    }
}