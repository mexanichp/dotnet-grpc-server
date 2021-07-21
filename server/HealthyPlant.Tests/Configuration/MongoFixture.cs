using System;
using System.Collections.Immutable;
using HealthyPlant.Data;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace HealthyPlant.Tests.Configuration
{
    public class MongoFixture : IDisposable
    {
        public IMongoRepository Repository { get; }
        public static ObjectId UserId => ObjectId.Parse("5f7c4375d0124f47bfcd6b3f");
        private readonly MongoClient _mongoClient;
        private readonly PlantsDbSettings _plantsDbSettings;

        public MongoFixture()
        {
            MappingConfig.RegisterMappings();
            DbConfiguration.ConfigureMongoDb();
            _plantsDbSettings = new PlantsDbSettings
            {
                DatabaseName = $"Test-{Guid.NewGuid()}",
                ConnectionString = "mongodb://localhost:27018/?authSource=admin",
                UsersCollectionName = "users",
                OldHistoryCollectionName = "old-history"
            };
            var settings = MongoClientSettings.FromConnectionString(_plantsDbSettings.ConnectionString);
            settings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(@event => Console.WriteLine(@event.Command.ToJson(new JsonWriterSettings{Indent = true})));
                cb.Subscribe<CommandSucceededEvent>(@event => Console.WriteLine(@event.Reply.ToJson(new JsonWriterSettings{Indent = true})));
            };
            _mongoClient = new MongoClient(settings);
            Repository = new MongoRepository(_mongoClient, _plantsDbSettings);
            var userTimezone = TimeSpan.FromHours(3);
            var userTodayDate = DateTime.SpecifyKind(DateTimeOffset.UtcNow.ToOffset(userTimezone).Date, DateTimeKind.Utc);
            var plants = new[]
            {
                new PlantDomain
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = "Monstera Bobby",
                    WateringStart = userTodayDate.AddDays(-2),
                    WateringPeriodicity = Domain.Plants.Periodicity.FourDays,
                    Notes =
                        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Ut gravida scelerisque nisi sit amet facilisis. Fusce eget dui auctor, eleifend nisl auctor,\nlacinia turpis. Nullam luctus massa nec ornare malesuada. Vestibulum tempor eget augue non aliquam. Phasellus lobortis magna urna, nec ornare ante condimentum a. Nulla et tristique mi. Morbi at ligula dapibus, luctus ex id, maximus quam. Quisque ultricies massa vitae urna lacinia, non facilisis tellus ultrices. Nunc a nunc sed justo dapibus aliquet. Cras accumsan porta felis, vitae rutrum nisi scelerisque vel. Vivamus vitae tempor quam, scelerisque semper odio. Praesent sodales condimentum sagittis. In at est nec dui luctus scelerisque. Mauris pretium nulla quis facilisis posuere.",
                    IconRef = "5",
                    FeedingStart = userTodayDate.AddDays(1),
                    FeedingPeriodicity = Domain.Plants.Periodicity.EachMonth,
                },
                new PlantDomain
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = "Succulent Harry",
                    WateringStart = userTodayDate.AddDays(-3),
                    WateringPeriodicity = Domain.Plants.Periodicity.ThreeDays,
                    Notes = "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                    IconRef = "4",
                    FeedingStart = userTodayDate.AddDays(-7),
                    FeedingPeriodicity = Domain.Plants.Periodicity.FiveDays,
                    MistingStart = userTodayDate.AddDays(5),
                    MistingPeriodicity = Domain.Plants.Periodicity.TwoDays
                }
            };
            var user = new UserDomain
            {
                Id = UserId.ToString(),
                Email = "test1@email.com",
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = UserId.ToString(),
                Timezone = userTimezone,
                Plants = plants.ToImmutableList()
            };

            Repository.UsersBson.ReplaceOne(user.GetFirebaseRefQueryBuilder(), user.WriteToBson(), new ReplaceOptions { IsUpsert = true });
        }

        public void Dispose()
        {
            _mongoClient?.DropDatabase(_plantsDbSettings.DatabaseName);
        }
    }
}