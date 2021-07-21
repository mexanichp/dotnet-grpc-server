using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using HealthyPlant.Data;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class DevMigrations
    {
        public static ObjectId UserId => ObjectId.Parse("5f7c4375d0124f47bfcd6b3f");

        public static async ValueTask PerformMigrationsAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var mongoClient = sp.GetRequiredService<IMongoClient>();
            var dbSettings = sp.GetRequiredService<PlantsDbSettings>();
            //await mongoClient.DropDatabaseAsync(dbSettings.DatabaseName);
            var db = mongoClient.GetDatabase(dbSettings.DatabaseName);
            var users = db.GetCollection<BsonDocument>(dbSettings.UsersCollectionName);
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

            await users.ReplaceOneAsync(user.GetFirebaseRefQueryBuilder(), user.WriteToBson(), new ReplaceOptions {IsUpsert = true});
            //await Fulfill10MRecordsAsync(plants, users);
        }

        private static async Task Fulfill10MRecordsAsync(PlantDomain[] plants, IMongoCollection<BsonDocument> users)
        {
            for (int i = 0; i < 200000; i++)
            {
                var u = Enumerable.Range(1, 50).Select(t => new UserDomain
                {
                    FirebaseRef = RndString,
                    Email = RndString,
                    Plants = plants.ToImmutableList(),
                    Timezone = TimeSpan.Zero,
                    DateFormat = "yyyy-MM-dd"
                }.WriteToBson());
                await users.InsertManyAsync(u);
            }
        }


        private static readonly Random Rnd = new Random();

        private static string RndString => new string(Enumerable
            .Repeat("AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz1234567890", 28)
            .Select(t => t[Rnd.Next(t.Length)]).ToArray());
    }
}