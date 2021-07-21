using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using HealthyPlant.Domain.History;
using HealthyPlant.Domain.Plants;
using Infrastructure.DateTimeExtensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Domain.Users
{
    public sealed record UserDomain
    {
        public const string FirebaseRegistrationTokensField = "firebase_registration_tokens";
        public const string LastNotificationTimeField = "last_notification_time";
        public const string LanguageField = "language";
        public const string NotificationTimeField = "notification_time";
        public const string TimezoneField = "timezone";
        public const string FirebaseRefField = "firebase_ref";
        public const string DateFormatField = "date_format";
        public const string PlantsField = "plants";
        public const string EmailField = "email";
        public const string IdField = "_id";
        private readonly string _id;
        private readonly string _email;
        private readonly string _dateFormat;
        private readonly string _firebaseRef;
        private readonly string _language;

        public UserDomain(
            string? id = null,
            string? email = null,
            ImmutableList<PlantDomain>? plants = null,
            string? dateFormat = null,
            string? firebaseRef = null,
            string? timezone = null,
            string? notificationTime = null,
            string? language = null,
            DateTimeOffset? lastNotificationTime = null,
            ImmutableList<string>? firebaseRegistrationTokens = null)
        {
            _id = id?.Trim() ?? "";
            _email = email?.Trim() ?? "";
            Plants = plants ?? ImmutableList<PlantDomain>.Empty;
            _dateFormat = dateFormat?.Trim() ?? "";
            _firebaseRef = firebaseRef?.Trim() ?? "";
            Timezone = TimeSpan.TryParse(timezone?.Trim(), out var tz) ? tz : default;
            NotificationTime = TimeSpan.TryParse(notificationTime?.Trim(), out var nt) ? nt : default;
            _language = language?.Trim() ?? "";
            LastNotificationTime = lastNotificationTime ?? default;
            FirebaseRegistrationTokens = firebaseRegistrationTokens ?? ImmutableList<string>.Empty;
        }

        public string Id
        {
            get => _id;
            init => _id = value.Trim();
        }

        public string Email
        {
            get => _email;
            init => _email = value.Trim();
        }

        public ImmutableList<PlantDomain> Plants { get; init; }

        public string DateFormat
        {
            get => _dateFormat;
            init => _dateFormat = value.Trim();
        }

        public string FirebaseRef
        {
            get => _firebaseRef;
            init => _firebaseRef = value.Trim();
        }

        public TimeSpan Timezone { get; init; }

        public TimeSpan NotificationTime { get; init; }

        public string Language
        {
            get => _language;
            init => _language = value.Trim();
        }

        public ImmutableList<string> FirebaseRegistrationTokens { get; init; }

        public Lazy<DateTimeOffset> TodayUserDate => new(DateTimeOffset.UtcNow.ToOffsetUtcDate(Timezone));

        public Lazy<DateTimeOffset> TodayUserTime => new(DateTimeOffset.UtcNow.ToOffsetUtcTime(Timezone));

        public DateTimeOffset LastNotificationTime { get; init; }

        public Lazy<ImmutableList<HistoryGroupDomain>> NowAndBeyond => new(() =>
        {
            var start = TodayUserDate.Value.AddDays(1);
            var end = TodayUserDate.Value.AddDays(14);
            var futureHistory = HistoryGroupDomain.GetHistoryGroups(Plants, start, end);
            var todayHistory = HistoryGroupDomain.GetHistoryGroupForUserToday(Plants, TodayUserDate.Value);
            if (todayHistory != null)
            {
                return futureHistory.Insert(0, todayHistory).Sort(HistoryGroupDomain.DateSortAscendingComparer);
            }

            return futureHistory.Sort(HistoryGroupDomain.DateSortAscendingComparer);
        });

        public Lazy<bool> IsTodayAnyHistory => new(() =>
        {
            var u = AddMissingHistory();
            var todayHistory = HistoryGroupDomain.GetHistoryGroupForUserToday(u.Plants, u.TodayUserDate.Value);
            return todayHistory != null;
        });

        public BsonDocument WriteToBson()
        {
            if (!ObjectId.TryParse(Id, out var id)) throw new ArgumentException($"Invalid Id {Id}", nameof(Id));

            var document = new BsonDocument();
            document.Set(IdField, new BsonObjectId(id));
            document.Set(EmailField, new BsonString(Email));
            document.Set(PlantsField, new BsonArray(Plants.Select(t => t.WriteToBson())));
            document.Set(DateFormatField, new BsonString(DateFormat));
            document.Set(FirebaseRefField, new BsonString(FirebaseRef));
            document.Set(TimezoneField, new BsonInt64((long) Timezone.TotalMilliseconds));
            document.Set(NotificationTimeField, new BsonInt64((long) NotificationTime.TotalMilliseconds));
            document.Set(LanguageField, new BsonString(Language));
            document.Set(LastNotificationTimeField, new BsonInt64(LastNotificationTime.ToUnixTimeMilliseconds()));
            document.Set(FirebaseRegistrationTokensField, new BsonArray(FirebaseRegistrationTokens));

            return document;
        }

        public static UserDomain ReadFromBson(BsonDocument document)
        {
            var userDomain = new UserDomain();

            if (document.TryGetValue(IdField, out var id) && id.IsObjectId)
            {
                userDomain = userDomain with {Id = id.AsObjectId.ToString()};
            }
            else throw new ArgumentException("UserDomain contains no _id element.");

            if (document.TryGetValue(EmailField, out var email) && email.IsString)
            {
                userDomain = userDomain with {Email = email.AsString};
            }

            if (document.TryGetValue(PlantsField, out var plants) && plants.IsBsonArray)
            {
                userDomain = userDomain with {Plants = plants.AsBsonArray.Values.Where(t => t.IsBsonDocument).Select(t => PlantDomain.ReadFromBson(t.AsBsonDocument)).ToImmutableList()};
            }

            if (document.TryGetValue(DateFormatField, out var dateFormat) && dateFormat.IsString)
            {
                var dbDateFormat = dateFormat.AsString;
                if (string.IsNullOrEmpty(dbDateFormat)) dbDateFormat = "yyyy-MM-dd";

                userDomain = userDomain with {DateFormat = dbDateFormat};
            }

            if (document.TryGetValue(FirebaseRefField, out var firebaseRef) && firebaseRef.IsString)
            {
                userDomain = userDomain with {FirebaseRef = firebaseRef.AsString};
            }
            else throw new ArgumentException("UserDomain contains no firebase_ref element.");

            if (document.TryGetValue(TimezoneField, out var timezone) && timezone.IsString && TimeSpan.TryParse(timezone.AsString, out var tz))
            {
                userDomain = userDomain with {Timezone = tz};
            }
            else if (timezone?.IsInt32 ?? false)
            {
                userDomain = userDomain with {Timezone = TimeSpan.FromMinutes(timezone.AsInt32)};
            }
            else if (timezone?.IsInt64 ?? false)
            {
                userDomain = userDomain with {Timezone = TimeSpan.FromMilliseconds(timezone.AsInt64)};
            }

            if (document.TryGetValue(LanguageField, out var language) && language.IsString && !string.IsNullOrEmpty(language.AsString))
            {
                userDomain = userDomain with {Language = language.AsString};
            }
            else
            {
                userDomain = userDomain with {Language = CultureInfo.GetCultureInfo("en").Name};
            }

            if (document.TryGetValue(NotificationTimeField, out var notificationTime) && notificationTime.IsString && TimeSpan.TryParse(notificationTime.AsString, out var nt))
            {
                userDomain = userDomain with {NotificationTime = nt};
            }
            else if (notificationTime?.IsInt64 ?? false)
            {
                userDomain = userDomain with {NotificationTime = TimeSpan.FromMilliseconds(notificationTime.AsInt64)};
            }
            else
            {
                userDomain = userDomain with {NotificationTime = TimeSpan.FromHours(10)};
            }

            if (document.TryGetValue(LastNotificationTimeField, out var lastNotificationTime) && lastNotificationTime.IsBsonDateTime)
            {
                userDomain = userDomain with {LastNotificationTime = DateTimeOffset.FromUnixTimeMilliseconds(lastNotificationTime.AsBsonDateTime.MillisecondsSinceEpoch)};
            }
            else if (lastNotificationTime?.IsInt64 ?? false)
            {
                userDomain = userDomain with {LastNotificationTime = DateTimeOffset.FromUnixTimeMilliseconds(lastNotificationTime.AsInt64)};
            }

            if (document.TryGetValue(FirebaseRegistrationTokensField, out var firebaseRegistrationTokens) && firebaseRegistrationTokens.IsBsonArray)
            {
                userDomain = userDomain with {FirebaseRegistrationTokens = firebaseRegistrationTokens.AsBsonArray.Values.Where(v => v.IsString).Select(t => t.AsString).ToImmutableList()};
            }

            return userDomain;
        }

        public bool Equals(UserDomain? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Email == other.Email && Plants.SequenceEqual(other.Plants) && DateFormat == other.DateFormat && FirebaseRef == other.FirebaseRef && Timezone.Equals(other.Timezone);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Email, Plants, DateFormat, FirebaseRef, Timezone);
        }

        public FilterDefinition<BsonDocument> GetFirebaseRefQueryBuilder()
        {
            if (string.IsNullOrEmpty(FirebaseRef)) throw new ArgumentException($"{nameof(FirebaseRef)} is null or empty.", nameof(FirebaseRef));

            return Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, string>(FirebaseRefField), FirebaseRef);
        }

        public UserDomain AddNewPlant(PlantDomain plantDomain)
        {
            var newPlant = plantDomain with {Id = ObjectId.GenerateNewId().ToString()};
            if (plantDomain.WateringStart == TodayUserDate.Value)
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Watering,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            if (plantDomain.MistingStart == TodayUserDate.Value)
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Misting,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            if (plantDomain.FeedingStart == TodayUserDate.Value)
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Feeding,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            if (plantDomain.RepottingStart == TodayUserDate.Value)
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Repotting,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            return this with {Plants = Plants.Add(newPlant)};
        }

        public UserDomain RemovePlant(string plantId)
        {
            return this with {Plants = Plants.RemoveAll(domain => domain.Id == plantId)};
        }

        public UserDomain AddNewUser()
        {
            return this with
            {
                Id = ObjectId.GenerateNewId().ToString(), 
                DateFormat = "yyyy-MM-dd",
                NotificationTime = TimeSpan.FromHours(10),
                LastNotificationTime = TodayUserTime.Value.AddHours(10 - DateTime.UtcNow.Hour)
            };
        }

        public UserDomain AddMissingHistory()
        {
            var domain = this;
            foreach (var p in Plants)
            {
                var plant = p;
                var lastWatering = plant.History.Where(t => t.Type == HistoryType.Watering).OrderByDescending(t => t.Date).FirstOrDefault()?.Date.AddDays(1) ?? plant.WateringStart;
                var lastMisting = plant.History.Where(t => t.Type == HistoryType.Misting).OrderByDescending(t => t.Date).FirstOrDefault()?.Date.AddDays(1) ?? plant.MistingStart;
                var lastFeeding = plant.History.Where(t => t.Type == HistoryType.Feeding).OrderByDescending(t => t.Date).FirstOrDefault()?.Date.AddDays(1) ?? plant.FeedingStart;
                var lastRepotting = plant.History.Where(t => t.Type == HistoryType.Repotting).OrderByDescending(t => t.Date).FirstOrDefault()?.Date.AddDays(1) ?? plant.RepottingStart;

                if (lastWatering != DateTimeOffset.UnixEpoch && lastWatering != default && lastWatering != TodayUserDate.Value.AddDays(1))
                {
                    var missingHistory = PlantHelper.CalculateDateRange(lastWatering, TodayUserDate.Value.AddDays(1), plant.WateringPeriodicity)
                        .Select(t => new HistoryDomain(
                            id: ObjectId.GenerateNewId().ToString(),
                            dateSeconds: t.ToUnixTimeSeconds(),
                            isDone: false,
                            type: HistoryType.Watering,
                            plantId: plant.Id,
                            plantName: plant.Name,
                            plantIconRef: plant.IconRef,
                            userId: Id))
                        .ToArray();

                    if (missingHistory.Length > 0)
                    {
                        var plantWithUpdatedHistory = plant with {History = plant.History.AddRange(missingHistory), OldHistory = plant.OldHistory.AddRange(missingHistory)};
                        domain = domain with {Plants = domain.Plants.Replace(plant, plantWithUpdatedHistory)};
                        plant = plantWithUpdatedHistory;
                    }
                }

                if (lastMisting != DateTimeOffset.UnixEpoch && lastMisting != default && lastMisting != TodayUserDate.Value.AddDays(1))
                {
                    var missingHistory = PlantHelper.CalculateDateRange(lastMisting, TodayUserDate.Value.AddDays(1), plant.MistingPeriodicity)
                        .Select(t => new HistoryDomain(
                            id: ObjectId.GenerateNewId().ToString(),
                            dateSeconds: t.ToUnixTimeSeconds(),
                            isDone: false,
                            type: HistoryType.Misting,
                            plantId: plant.Id,
                            plantName: plant.Name,
                            plantIconRef: plant.IconRef,
                            userId: Id))
                        .ToArray();

                    if (missingHistory.Length > 0)
                    {
                        var plantWithUpdatedHistory = plant with {History = plant.History.AddRange(missingHistory), OldHistory = plant.OldHistory.AddRange(missingHistory)};
                        domain = domain with {Plants = domain.Plants.Replace(plant, plantWithUpdatedHistory)};
                        plant = plantWithUpdatedHistory;
                    }
                }

                if (lastFeeding != DateTimeOffset.UnixEpoch && lastFeeding != default && lastFeeding != TodayUserDate.Value.AddDays(1))
                {
                    var missingHistory = PlantHelper.CalculateDateRange(lastFeeding, TodayUserDate.Value.AddDays(1), plant.FeedingPeriodicity)
                        .Select(t => new HistoryDomain(id:
                            ObjectId.GenerateNewId().ToString(),
                            dateSeconds: t.ToUnixTimeSeconds(),
                            isDone: false,
                            type: HistoryType.Feeding,
                            plantId: plant.Id,
                            plantName: plant.Name,
                            plantIconRef: plant.IconRef,
                            userId: Id))
                        .ToArray();

                    if (missingHistory.Length > 0)
                    {
                        var plantWithUpdatedHistory = plant with {History = plant.History.AddRange(missingHistory), OldHistory = plant.OldHistory.AddRange(missingHistory)};
                        domain = domain with {Plants = domain.Plants.Replace(plant, plantWithUpdatedHistory)};
                        plant = plantWithUpdatedHistory;
                    }
                }

                if (lastRepotting != DateTimeOffset.UnixEpoch && lastRepotting != default && lastRepotting != TodayUserDate.Value.AddDays(1))
                {
                    var missingHistory = PlantHelper.CalculateDateRange(lastRepotting, TodayUserDate.Value.AddDays(1), plant.RepottingPeriodicity)
                        .Select(t => new HistoryDomain(
                            id: ObjectId.GenerateNewId().ToString(),
                            dateSeconds: t.ToUnixTimeSeconds(),
                            isDone: false,
                            type: HistoryType.Repotting,
                            plantId: plant.Id,
                            plantName: plant.Name,
                            plantIconRef: plant.IconRef,
                            userId: Id))
                        .ToArray();

                    if (missingHistory.Length > 0)
                    {
                        var plantWithUpdatedHistory = plant with {History = plant.History.AddRange(missingHistory), OldHistory = plant.OldHistory.AddRange(missingHistory)};
                        domain = domain with {Plants = domain.Plants.Replace(plant, plantWithUpdatedHistory)};
                        plant = plantWithUpdatedHistory;
                    }
                }
            }

            return domain;
        }

        public UserDomain UpdateHistory(string id, bool isDone)
        {
            var user = this;
            foreach (PlantDomain plant in Plants)
            foreach (HistoryDomain history in plant.History)
            {
                if (history.Id == id)
                {
                    user = user with {Plants = Plants.Replace(plant, plant with {History = plant.History.Replace(history, history with {IsDone = isDone})})};
                    break;
                }
            }

            return user;
        }

        public UserDomain UpdatePlant(PlantDomain plant)
        {
            var oldPlant = Plants.Find(p => p.Id == plant.Id);
            if (oldPlant == null)
            {
                return this;
            }

            var newPlant = oldPlant with
            {
                Name = plant.Name,
                Notes = plant.Notes,
                IconRef = plant.IconRef,
                WateringStart = plant.WateringStart,
                WateringPeriodicity = plant.WateringPeriodicity,
                MistingStart = plant.MistingStart,
                MistingPeriodicity = plant.MistingPeriodicity,
                FeedingStart = plant.FeedingStart,
                FeedingPeriodicity = plant.FeedingPeriodicity,
                RepottingStart = plant.RepottingStart,
                RepottingPeriodicity = plant.RepottingPeriodicity
            };

            if (newPlant.WateringStart == TodayUserDate.Value && !newPlant.History.Any(h => h.Date == TodayUserDate.Value && h.Type == HistoryType.Watering))
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Watering,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            if (newPlant.MistingStart == TodayUserDate.Value && !newPlant.History.Any(h => h.Date == TodayUserDate.Value && h.Type == HistoryType.Misting))
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Misting,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            if (newPlant.FeedingStart == TodayUserDate.Value && !newPlant.History.Any(h => h.Date == TodayUserDate.Value && h.Type == HistoryType.Feeding))
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Feeding,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            if (newPlant.RepottingStart == TodayUserDate.Value && !newPlant.History.Any(h => h.Date == TodayUserDate.Value && h.Type == HistoryType.Repotting))
            {
                newPlant = newPlant with
                {
                    History = newPlant.History.Add(new HistoryDomain
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Type = HistoryType.Repotting,
                        Date = TodayUserDate.Value,
                        IsDone = false
                    })
                };
            }

            return this with {Plants = Plants.Replace(oldPlant, newPlant)};
        }

        public bool TryGetOldHistory(out ImmutableList<HistoryDomain> oldHistory)
        {
            oldHistory = ImmutableList<HistoryDomain>.Empty;
            var result = false;
            foreach (var plant in Plants)
            foreach (var history in plant.OldHistory)
            {
                result = true;
                oldHistory = oldHistory.Add(history with {PlantIconRef = plant.IconRef, PlantId = plant.Id, PlantName = plant.Name, UserId = Id});
            }

            return result;
        }
    }
}