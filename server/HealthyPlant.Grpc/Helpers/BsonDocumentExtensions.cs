using System.Collections.Generic;
using System.Diagnostics.Contracts;
using MongoDB.Bson;

namespace HealthyPlant.Grpc.Helpers
{
    public static class BsonDocumentExtensions
    {
        [Pure]
        public static Dictionary<string, object> ToRootDictionary(this BsonDocument document)
        {
            var d = document;
            var res = new Dictionary<string, object>();
            foreach (var bsonVal in d)
            {
                switch (bsonVal.Value.BsonType)
                {
                    case BsonType.Boolean:
                        res.Add(bsonVal.Name, bsonVal.Value.ToBoolean());
                        break;

                    case BsonType.DateTime:
                        res.Add(bsonVal.Name, bsonVal.Value.ToUniversalTime());
                        break;

                    case BsonType.Decimal128:
                        res.Add(bsonVal.Name, bsonVal.Value.ToDecimal());
                        break;

                    case BsonType.Double:
                        res.Add(bsonVal.Name, bsonVal.Value.ToDouble());
                        break;

                    case BsonType.Int32:
                        res.Add(bsonVal.Name, bsonVal.Value.ToInt32());
                        break;

                    case BsonType.Int64:
                        res.Add(bsonVal.Name, bsonVal.Value.ToInt64());
                        break;

                    case BsonType.ObjectId:
                        res.Add(bsonVal.Name, bsonVal.Value.AsObjectId.ToString());
                        break;

                    case BsonType.String:
                        res.Add(bsonVal.Name, bsonVal.Value.AsString);
                        break;

                    case BsonType.Timestamp:
                        res.Add(bsonVal.Name, bsonVal.Value.AsBsonTimestamp.Timestamp);
                        break;


                    default:
                        continue;
                }

            }

            return res;
        }
    }
}