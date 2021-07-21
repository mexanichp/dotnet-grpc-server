using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace HealthyPlant.Domain.History
{
    public sealed partial record HistoryDomain
    {
        private readonly string _id;
        private readonly string _plantIconRef;
        private readonly string _plantName;
        private readonly string _plantId;
        private readonly string _userId;

        public HistoryDomain(
            string? id = null,
            long dateSeconds = 0,
            bool isDone = false,
            HistoryType type = HistoryType.None,
            string? plantIconRef = null,
            string? plantName = null,
            string? plantId = null,
            string? userId = null)
        {
            _id = id ?? "";
            Date = DateTimeOffset.FromUnixTimeMilliseconds(dateSeconds * 1000);
            IsDone = isDone;
            Type = type;
            _plantIconRef = plantIconRef ?? "";
            _plantName = plantName ?? "";
            _plantId = plantId ?? "";
            _userId = userId ?? "";
        }

        public string Id
        {
            get => _id;
            init => _id = value.Trim();
        }

        public string UserId
        {
            get => _userId;
            init => _userId = value.Trim();
        }

        public DateTimeOffset Date { get; init; }

        public bool IsDone { get; init; }

        public HistoryType Type { get; init; }

        public string PlantIconRef
        {
            get => _plantIconRef;
            init => _plantIconRef = value.Trim();
        }

        public string PlantName
        {
            get => _plantName;
            init => _plantName = value.Trim();
        }

        public string PlantId
        {
            get => _plantId;
            init => _plantId = value.Trim();
        }

        public BsonDocument WriteToBson()
        {
            if (Type == HistoryType.None) throw new ArgumentException($"Invalid Type {Type}", nameof(Type));
            if (!ObjectId.TryParse(Id, out var id) || id == ObjectId.Empty) throw new ArgumentException($"Invalid Id {Id}", nameof(Id));

            var document = new BsonDocument();
            document.Set("_id", new BsonObjectId(id));
            document.Set("date", new BsonInt64(Date.ToUnixTimeMilliseconds()));
            document.Set("is_done", new BsonBoolean(IsDone));
            document.Set("type", new BsonInt32((int) Type));

            return document;
        }

        public static HistoryDomain ReadFromBson(BsonDocument document)
        {
            var domain = new HistoryDomain();

            if (document.TryGetValue("_id", out var id) && id.IsObjectId)
            {
                domain = domain with {Id = id.AsObjectId.ToString()};
            }
            else
            {
                throw new ArgumentException("HistoryDomain contains no _id element.");
            }

            if (document.TryGetValue("date", out var date) && date.IsBsonDateTime)
            {
                domain = domain with {Date = DateTimeOffset.FromUnixTimeMilliseconds(date.AsBsonDateTime.MillisecondsSinceEpoch)};
            }
            else if (date?.IsInt64 ?? false)
            {
                domain = domain with {Date = DateTimeOffset.FromUnixTimeMilliseconds(date.AsInt64)};
            }
            else
            {
                throw new ArgumentException("HistoryDomain contains no date element.");
            }

            if (document.TryGetValue("is_done", out var isDone) && isDone.IsBoolean)
            {
                domain = domain with {IsDone = isDone.ToBoolean()};
            }
            else
            {
                throw new ArgumentException("HistoryDomain contains no is_done element.");
            }

            if (document.TryGetValue("type", out var type) && type.IsInt32)
            {
                domain = domain with {Type = (HistoryType) type.ToInt32()};
            }
            else
            {
                throw new ArgumentException("HistoryDomain contains no type element.");
            }

            return domain;
        }

        

        public bool Equals(HistoryDomain? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Date.Equals(other.Date) && IsDone == other.IsDone && Type == other.Type;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Date, IsDone, (int) Type);
        } 

        public static IComparer<HistoryDomain> DateSortDescendingComparer { get; } = new DateSortDescComparer();

        private sealed class DateSortDescComparer : IComparer<HistoryDomain>
        {
            public int Compare(HistoryDomain? x, HistoryDomain? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, x)) return 1;
                if (ReferenceEquals(null, y)) return -1;

                return y.Date.CompareTo(x.Date);
            }
        }
    }

    public enum HistoryType
    {
        None = -1,
        Watering = 1,
        Misting = 2,
        Feeding = 3,
        Repotting = 4
    }
}