using System;
using System.Collections.Immutable;
using System.Linq;
using HealthyPlant.Domain.History;
using HealthyPlant.Domain.Infrastructure;
using MongoDB.Bson;

namespace HealthyPlant.Domain.Plants
{
    public sealed record PlantDomain
    {
        private readonly ImmutableList<HistoryDomain> _history;
        private readonly ImmutableList<HistoryDomain> _oldHistory;
        private Lazy<DateTimeOffset>? _wateringNext;
        private Lazy<DateTimeOffset>? _mistingNext;
        private Lazy<DateTimeOffset>? _feedingNext;
        private Lazy<DateTimeOffset>? _repottingNext;
        private readonly string _id;
        private readonly string _name;
        private readonly string _notes;
        private readonly string _iconRef;

        public PlantDomain(
            string? id = null,
            string? name = null,
            string? notes = null,
            string? iconRef = null,
            long wateringStartSeconds = 0,
            ushort wateringPeriodicity = 0,
            long mistingStartSeconds = 0,
            ushort mistingPeriodicity = 0,
            long feedingStartSeconds = 0,
            ushort feedingPeriodicity = 0,
            long repottingStartSeconds = 0,
            ushort repottingPeriodicity = 0,
            ImmutableList<HistoryDomain>? history = null,
            DateTimeOffset todayUserDate = default)
        {
            _id = id ?? "";
            _name = name ?? "";
            _notes = notes ?? "";
            _iconRef = iconRef ?? "";
            WateringStart = DateTimeOffset.FromUnixTimeMilliseconds(wateringStartSeconds * 1000);
            WateringPeriodicity = (Periodicity) wateringPeriodicity;
            MistingStart = DateTimeOffset.FromUnixTimeMilliseconds(mistingStartSeconds * 1000);
            MistingPeriodicity = (Periodicity) mistingPeriodicity;
            FeedingStart = DateTimeOffset.FromUnixTimeMilliseconds(feedingStartSeconds * 1000);
            FeedingPeriodicity = (Periodicity) feedingPeriodicity;
            RepottingStart = DateTimeOffset.FromUnixTimeMilliseconds(repottingStartSeconds * 1000);
            RepottingPeriodicity = (Periodicity) repottingPeriodicity;
            _history = GetRecentHistory(history);
            _oldHistory = GetOldHistory(history);
            TodayUserDate = todayUserDate == default ? DateTimeOffset.UtcNow.Date : todayUserDate;
        }

        public string Id
        {
            get => _id;
            init => _id = value.Trim();
        }

        public string Name
        {
            get => _name;
            init => _name = value.Trim();
        }

        public string Notes
        {
            get => _notes;
            init => _notes = value.Trim();
        }

        public string IconRef
        {
            get => _iconRef;
            init => _iconRef = value.Trim();
        }

        public DateTimeOffset WateringStart { get; init; }

        public Periodicity WateringPeriodicity { get; init; }

        public DateTimeOffset MistingStart { get; init; }

        public Periodicity MistingPeriodicity { get; init; }

        public DateTimeOffset FeedingStart { get; init; }

        public Periodicity FeedingPeriodicity { get; init; }

        public DateTimeOffset RepottingStart { get; init; }

        public Periodicity RepottingPeriodicity { get; init; }

        public ImmutableList<HistoryDomain> History
        {
            get => _history.Sort(HistoryDomain.DateSortDescendingComparer);
            init => _history = GetRecentHistory(value);
        }

        public ImmutableList<HistoryDomain> OldHistory
        {
            get => _oldHistory;
            init => _oldHistory = GetOldHistory(value);
        }

        public DateTimeOffset TodayUserDate { get; }

        public Lazy<DateTimeOffset> WateringNext
        {
            get
            {
                _wateringNext ??= new Lazy<DateTimeOffset>(DateService.GetNextDate(WateringPeriodicity, TodayUserDate, WateringStart));
                return _wateringNext;
            }
        }

        public Lazy<DateTimeOffset> MistingNext
        {
            get
            {
                _mistingNext ??= new Lazy<DateTimeOffset>(DateService.GetNextDate(MistingPeriodicity, TodayUserDate, MistingStart));
                return _mistingNext;
            }
        }

        public Lazy<DateTimeOffset> FeedingNext
        {
            get
            {
                _feedingNext ??= new Lazy<DateTimeOffset>(DateService.GetNextDate(FeedingPeriodicity, TodayUserDate, FeedingStart));
                return _feedingNext;
            }
        }

        public Lazy<DateTimeOffset> RepottingNext
        {
            get
            {
                _repottingNext ??= new Lazy<DateTimeOffset>(DateService.GetNextDate(RepottingPeriodicity, TodayUserDate, RepottingStart));
                return _repottingNext;
            }
        }

        public BsonDocument WriteToBson()
        {
            if (!ObjectId.TryParse(Id, out var id)) throw new ArgumentException($"Invalid Id {Id}", nameof(Id));

            var document = new BsonDocument();

            document.Set("_id", new BsonObjectId(id));
            document.Set("name", new BsonString(Name));
            document.Set("notes", new BsonString(Notes));
            document.Set("icon_ref", new BsonString(IconRef));
            document.Set("watering_start", new BsonInt64(WateringStart.ToUnixTimeMilliseconds()));
            document.Set("watering_periodicity", new BsonInt32((int) WateringPeriodicity));
            document.Set("misting_start", new BsonInt64(MistingStart.ToUnixTimeMilliseconds()));
            document.Set("misting_periodicity", new BsonInt32((int) MistingPeriodicity));
            document.Set("feeding_start", new BsonInt64(FeedingStart.ToUnixTimeMilliseconds()));
            document.Set("feeding_periodicity", new BsonInt32((int) FeedingPeriodicity));
            document.Set("repotting_start", new BsonInt64(RepottingStart.ToUnixTimeMilliseconds()));
            document.Set("repotting_periodicity", new BsonInt32((int) RepottingPeriodicity));
            document.Set("history", new BsonArray(History.Select(t => t.WriteToBson())));

            return document;
        }

        public static PlantDomain ReadFromBson(BsonDocument document)
        {
            var plantDomain = new PlantDomain();

            plantDomain = ReadId(document, plantDomain);
            plantDomain = ReadName(document, plantDomain);
            plantDomain = ReadNotes(document, plantDomain);
            plantDomain = ReadIconRef(document, plantDomain);
            plantDomain = ReadWatering(document, plantDomain);
            plantDomain = ReadMisting(document, plantDomain);
            plantDomain = ReadFeeding(document, plantDomain);
            plantDomain = ReadRepotting(document, plantDomain);
            plantDomain = ReadHistory(document, plantDomain);

            return plantDomain;
        }

        public bool Equals(PlantDomain? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Name == other.Name && Notes == other.Notes && IconRef == other.IconRef && WateringStart.Equals(other.WateringStart) && WateringPeriodicity == other.WateringPeriodicity && MistingStart.Equals(other.MistingStart) && MistingPeriodicity == other.MistingPeriodicity && FeedingStart.Equals(other.FeedingStart) && FeedingPeriodicity == other.FeedingPeriodicity && RepottingStart.Equals(other.RepottingStart) && RepottingPeriodicity == other.RepottingPeriodicity && History.SequenceEqual(other.History);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Id);
            hashCode.Add(Name);
            hashCode.Add(Notes);
            hashCode.Add(IconRef);
            hashCode.Add(WateringStart);
            hashCode.Add((int) WateringPeriodicity);
            hashCode.Add(MistingStart);
            hashCode.Add((int) MistingPeriodicity);
            hashCode.Add(FeedingStart);
            hashCode.Add((int) FeedingPeriodicity);
            hashCode.Add(RepottingStart);
            hashCode.Add((int) RepottingPeriodicity);
            hashCode.Add(History);
            return hashCode.ToHashCode();
        }

        private ImmutableList<HistoryDomain> GetOldHistory(ImmutableList<HistoryDomain>? history)
            => history?.Where(h => h.Date < TodayUserDate.AddMonths(-1)).ToImmutableList()
               ?? ImmutableList<HistoryDomain>.Empty;

        private ImmutableList<HistoryDomain> GetRecentHistory(ImmutableList<HistoryDomain>? history)
            => history?.Where(h => h.Date >= TodayUserDate.AddMonths(-1)).ToImmutableList()
               ?? ImmutableList<HistoryDomain>.Empty;

        private static PlantDomain ReadId(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("_id", out var id) && id.IsObjectId)
            {
                plantDomain = plantDomain with {Id = id.AsObjectId.ToString()};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no _id element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadName(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("name", out var name) && name.IsString)
            {
                plantDomain = plantDomain with {Name = name.AsString};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no name element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadNotes(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("notes", out var notes) && notes.IsString)
            {
                plantDomain = plantDomain with {Notes = notes.AsString};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no notes element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadIconRef(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("icon_ref", out var iconRef) && iconRef.IsString)
            {
                plantDomain = plantDomain with {IconRef = iconRef.AsString};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no icon_ref element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadWatering(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("watering_start", out var start) && start.IsBsonDateTime)
            {
                plantDomain = plantDomain with {WateringStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsBsonDateTime.MillisecondsSinceEpoch)};
            }
            else if (start?.IsInt64 ?? false)
            {
                plantDomain = plantDomain with {WateringStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsInt64)};
            }
            else
            {
                plantDomain = plantDomain with {WateringStart = DateTimeOffset.FromUnixTimeMilliseconds(0)};
            }

            if (document.TryGetValue("watering_periodicity", out var periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {WateringPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else if (document.TryGetValue("watering_days", out periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {WateringPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no watering_periodicity element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadMisting(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("misting_start", out var start) && start.IsBsonDateTime)
            {
                plantDomain = plantDomain with {MistingStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsBsonDateTime.MillisecondsSinceEpoch)};
            }
            else if (start?.IsInt64 ?? false)
            {
                plantDomain = plantDomain with { MistingStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsInt64) };
            }
            else
            {
                plantDomain = plantDomain with {MistingStart = DateTimeOffset.FromUnixTimeMilliseconds(0)};
            }

            if (document.TryGetValue("misting_periodicity", out var periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {MistingPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else if (document.TryGetValue("misting_days", out periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {MistingPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no misting_periodicity element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadFeeding(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("feeding_start", out var start) && start.IsBsonDateTime)
            {
                plantDomain = plantDomain with {FeedingStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsBsonDateTime.MillisecondsSinceEpoch)};
            }
            else if (start?.IsInt64 ?? false)
            {
                plantDomain = plantDomain with { FeedingStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsInt64) };
            }
            else
            {
                plantDomain = plantDomain with {FeedingStart = DateTimeOffset.FromUnixTimeMilliseconds(0)};
            }

            if (document.TryGetValue("feeding_periodicity", out var periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {FeedingPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else if (document.TryGetValue("feeding_days", out periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {FeedingPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no feeding_periodicity element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadRepotting(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("repotting_start", out var start) && start.IsBsonDateTime)
            {
                plantDomain = plantDomain with {RepottingStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsBsonDateTime.MillisecondsSinceEpoch)};
            }
            if (start?.IsInt64 ?? false)
            {
                plantDomain = plantDomain with { RepottingStart = DateTimeOffset.FromUnixTimeMilliseconds(start.AsInt64) };
            }
            else
            {
                plantDomain = plantDomain with {RepottingStart = DateTimeOffset.FromUnixTimeMilliseconds(0)};
            }

            if (document.TryGetValue("repotting_periodicity", out var periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {RepottingPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else if (document.TryGetValue("feeding_days", out periodicity) && periodicity.IsInt32)
            {
                plantDomain = plantDomain with {RepottingPeriodicity = MapIntToPeriodicity(periodicity.AsInt32)};
            }
            else
            {
                throw new ArgumentException("PlantDomain contains no repotting_periodicity element.");
            }

            return plantDomain;
        }

        private static PlantDomain ReadHistory(BsonDocument document, PlantDomain plantDomain)
        {
            if (document.TryGetValue("history", out var history) && history.IsBsonArray)
            {
                var historyDomains = history.AsBsonArray.Values.Where(t => t.IsBsonDocument).Select(t => HistoryDomain.ReadFromBson(t.AsBsonDocument)).ToImmutableList();
                plantDomain = plantDomain with {History = historyDomains, OldHistory = historyDomains};
            }

            return plantDomain;
        }

        private static Periodicity MapIntToPeriodicity(int enumInt) => enumInt switch
        {
            1 => Periodicity.EachDay,
            2 => Periodicity.TwoDays,
            3 => Periodicity.ThreeDays,
            4 => Periodicity.FourDays,
            5 => Periodicity.FiveDays,
            6 => Periodicity.SixDays,
            7 => Periodicity.EachWeek,
            10 => Periodicity.TenDays,
            14 => Periodicity.TwoWeeks,
            21 => Periodicity.ThreeWeeks,
            101 => Periodicity.EachMonth,
            106 => Periodicity.SixMonths,
            1001 => Periodicity.EachYear,
            1002 => Periodicity.TwoYears,
            _ => Periodicity.ThreeWeeks
        };
    }

    public enum Periodicity
    {
        None = 0,
        EachDay = 1,
        TwoDays = 2,
        ThreeDays = 3,
        FourDays = 4,
        FiveDays = 5,
        SixDays = 6,
        EachWeek = 7,
        TenDays = 10,
        TwoWeeks = 14,
        ThreeWeeks = 21,
        EachMonth = 101,
        SixMonths = 106,
        EachYear = 1001,
        TwoYears = 1002,
    }
}