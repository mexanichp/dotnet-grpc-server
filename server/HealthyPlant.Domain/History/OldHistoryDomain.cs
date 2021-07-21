using System;
using MongoDB.Bson;

namespace HealthyPlant.Domain.History
{
    public sealed partial record HistoryDomain
    {
        public BsonDocument WriteToBsonOldHistory()
        {
            if (!ObjectId.TryParse(UserId, out var userId) || userId == ObjectId.Empty) throw new ArgumentException($"Invalid UserId {UserId}", nameof(UserId));
            if (!ObjectId.TryParse(PlantId, out var plantId) || plantId == ObjectId.Empty) throw new ArgumentException($"Invalid PlantId {PlantId}", nameof(PlantId));

            var document = WriteToBson();

            document.Set("user_id", new BsonObjectId(userId));
            document.Set("plant_id", new BsonObjectId(plantId));
            document.Set("plant_icon_ref", new BsonString(PlantIconRef));
            document.Set("plant_name", new BsonString(PlantName));

            return document;
        }

        public static HistoryDomain ReadFromBsonOldHistory(BsonDocument document)
            => ReadFromBson(document) with
            {
                UserId = ReadUserId(document),
                PlantIconRef = ReadPlantIconRef(document),
                PlantId = ReadPlantId(document),
                PlantName = ReadPlantName(document)
            };

        private static string ReadUserId(BsonDocument document)
        {
            if (document.TryGetValue("user_id", out var userId) && userId.IsObjectId)
            {
                return userId.AsObjectId.ToString();
            }

            throw new ArgumentException($"Document doesn't contain {nameof(userId)}", nameof(UserId));
        }

        private static string ReadPlantIconRef(BsonDocument document)
        {
            if (document.TryGetValue("plant_icon_ref", out var plantIconRef) && plantIconRef.IsString)
            {
                return plantIconRef.AsString;
            }

            throw new ArgumentException($"Document doesn't contain {nameof(plantIconRef)}", nameof(PlantIconRef));
        }

        private static string ReadPlantId(BsonDocument document)
        {
            if (document.TryGetValue("plant_id", out var plantId) && plantId.IsObjectId)
            {
                return plantId.AsObjectId.ToString();
            }

            throw new ArgumentException($"Document doesn't contain {nameof(plantId)}", nameof(PlantId));
        }

        private static string ReadPlantName(BsonDocument document)
        {
            if (document.TryGetValue("plant_name", out var plantName) && plantName.IsString)
            {
                return plantName.AsString;
            }

            throw new ArgumentException($"Document doesn't contain {nameof(plantName)}", nameof(PlantName));
        }
    }
}