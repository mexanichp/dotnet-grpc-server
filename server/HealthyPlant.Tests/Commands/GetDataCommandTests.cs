using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using HealthyPlant.Data;
using HealthyPlant.Domain.History;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc;
using HealthyPlant.Grpc.Commands.GetData;
using HealthyPlant.Grpc.Helpers;
using HealthyPlant.Grpc.Models;
using HealthyPlant.Tests.Configuration;
using Infrastructure.DateTimeExtensions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using HistoryType = HealthyPlant.Domain.History.HistoryType;
using Periodicity = HealthyPlant.Domain.Plants.Periodicity;

namespace HealthyPlant.Tests.Commands
{
    public class GetDataCommandTests
    {
        private GetDataCommandHandler _command;
        private MongoFixture _mongo;
        public IMongoRepository Repository { get; set; }

        [SetUp]
        public void Setup()
        {
            _mongo = new MongoFixture();
            Repository = _mongo.Repository;
            _command = new GetDataCommandHandler(Repository, new NullLogger<GetDataCommandHandler>());
        }

        [TearDown]
        public void Teardown()
        {
            _mongo?.Dispose();
        }

        [Test]
        public async Task Handle_HistoryIsNotUpToDate_ShouldUpdateHistoryWithCorrectData()
        {
            // Arrange
            var firebaseUser = new FirebaseUser(MongoFixture.UserId.ToString(), null, null);
            var plantName = "CE7B331F-050A-4942-AB63-DAAD8ADDA9A1";
            var userDomain = new UserDomain(firebaseRef: firebaseUser.Uid, timezone: "3:00");
            var utcNowDate = DateTimeOffset.UtcNow.ToOffsetUtcDate(userDomain.Timezone);
            var plant = new PlantDomain
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = plantName,
                Notes = "",
                IconRef = "4",
                FeedingStart = utcNowDate.AddDays(-13),
                FeedingPeriodicity = Periodicity.EachDay,
                WateringStart = utcNowDate.AddDays(-1),
                WateringPeriodicity = Periodicity.EachDay,
            };
            var u = Builders<BsonDocument>.Update.Push("plants", plant.WriteToBson());
            await Repository.UsersBson.UpdateOneAsync(userDomain.GetFirebaseRefQueryBuilder(), u);

            // Act
            var response = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);

            // Assert
            var todayNowAndBeyond = response.User.NowAndBeyond.Single(t => t.Date == (utcNowDate.ToTimestamp()));
            var responsePlant = response.User.Plants.Single(p => p.Name.Equals(plantName));

            // Ascending order
            response.User.NowAndBeyond.Should().BeInAscendingOrder(t => t.Date);

            // Not empty collection
            todayNowAndBeyond.FeedingHistory.Should().NotBeEmpty();
            todayNowAndBeyond.FeedingHistory.First().PlantIconRef.Should().NotBeEmpty();
            todayNowAndBeyond.FeedingHistory.First().PlantName.Should().Be(plantName);
            todayNowAndBeyond.WateringHistory.Should().NotBeEmpty();
            todayNowAndBeyond.MistingHistory.Should().BeEmpty();
            todayNowAndBeyond.RepottingHistory.Should().BeEmpty();

            // Today collection contains ids
            todayNowAndBeyond.FeedingHistory.Should().Match(t => t.All(x => IsObjectId(x.Id)));

            // Count of inserted history is correct
            responsePlant.History.Should().HaveCount(16);
            responsePlant.History.Should().BeInDescendingOrder(t => t.Date);

            // Today history has correct data
            responsePlant.History.First().Date.Should().Be(utcNowDate.ToTimestamp());
            responsePlant.History.First().PlantIconRef.Should().Be(plant.IconRef);
            responsePlant.History.First().PlantName.Should().Be(plantName);
            responsePlant.FeedingDays.Should().Be(Periodicity.EachDay);
        }

        [Test]
        public async Task Handle_HistoryIsNotUpToDateAndInvokedSeveralTimes_ShouldUpdateHistoryOnce()
        {
            // Arrange
            var firebaseRef = "80DD854D-5388-434F-9B63-524ED5B3E3ED";
            var firebaseUser = new FirebaseUser(firebaseRef, null, null);
            var todayTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.Date);
            var plantName = "1859D50E-0779-4EDF-8374-FD791DD17570";
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test@mail.com",
                Id = ObjectId.Parse("5ff5b9ea2795f8112cd8c35d").ToString(),
                Plants = new[]
                {
                    new PlantDomain
                    {
                        Id = ObjectId.Parse("5ff5b9fe2795f8112cd8c360").ToString(),
                        Name = plantName,
                        Notes = "",
                        IconRef = "4",
                        FeedingStart = todayTimestamp.AddDays(-1).ToDateTime(),
                        FeedingPeriodicity = Domain.Plants.Periodicity.EachDay,
                    }
                }.ToImmutableList(),
                Timezone = TimeSpan.FromHours(2)
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            var response = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);

            // Assert
            var todayNowAndBeyond = response.User.NowAndBeyond.Single(t => t.Date == todayTimestamp);
            var responsePlant = response.User.Plants.Single(p => p.Name.Equals(plantName));

            // Ascending order
            response.User.NowAndBeyond.Should().BeInAscendingOrder(t => t.Date);

            // Not empty collection
            todayNowAndBeyond.FeedingHistory.Should().NotBeEmpty();
            todayNowAndBeyond.FeedingHistory.First().PlantIconRef.Should().NotBeEmpty();
            todayNowAndBeyond.FeedingHistory.First().PlantName.Should().Be(plantName);
            todayNowAndBeyond.WateringHistory.Should().BeEmpty();
            todayNowAndBeyond.MistingHistory.Should().BeEmpty();
            todayNowAndBeyond.RepottingHistory.Should().BeEmpty();

            // Today collection contains ids
            todayNowAndBeyond.FeedingHistory.Should().Match(t => t.All(x => IsObjectId(x.Id)));

            // Count of inserted history is correct
            responsePlant.History.Should().HaveCount(2);
            responsePlant.History.Should().BeInDescendingOrder(t => t.Date);

            // Today history has correct data
            responsePlant.History.First().Date.Should().Be(todayTimestamp);
            responsePlant.History.First().PlantIconRef.Should().BeEmpty();
            responsePlant.History.First().PlantName.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_HistoryIsOlderThanOneMonths_ShouldUpdateOldHistoryCollection()
        {
            // Arrange
            var firebaseRef = "70DD854D-5388-434F-9B63-524ED5B3E3ED";
            var firebaseUser = new FirebaseUser(firebaseRef, null, null);
            var todayTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.Date);
            var plantName = "my plant name";
            var plant = new PlantDomain
            {
                Id = ObjectId.Parse("5ff5a4d9375ec94c83ef9847").ToString(),
                Name = plantName,
                WateringStart = DateTimeOffset.UtcNow.AddMonths(-1).AddDays(-1),
                WateringPeriodicity = Periodicity.EachDay,
                FeedingStart = DateTimeOffset.UtcNow.AddMonths(-1).AddDays(-2),
                FeedingPeriodicity = Periodicity.EachDay,
                IconRef = "4"
            };
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test@mail.com",
                Id = ObjectId.Parse("5ff5a4d9375ec94c83ef9842").ToString(),
                Plants = new[] {plant}.ToImmutableList(),
                Timezone = TimeSpan.FromHours(2)
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            var result = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            var cursor = await Repository.OldHistoryBson.FindAsync(Builders<BsonDocument>.Filter.Empty);
            var historyBson = await cursor.ToListAsync();
            var oldHistory = historyBson.Select(HistoryDomain.ReadFromBsonOldHistory).ToArray();

            // Assert
            oldHistory.Should().HaveCount(3);
            oldHistory.Should().Match(domains => domains.All(h => h.UserId == user.Id));
            oldHistory.Should().Match(domains => domains.All(h => h.PlantId == plant.Id));
            oldHistory.Should().Match(domains => domains.All(h => h.PlantIconRef== plant.IconRef));
            oldHistory.Should().Match(domains => domains.All(h => h.PlantName== plant.Name));
        }

        [Test]
        public async Task Handle_HistoryIsOlderThanOneMonthsAndPeriodicityIsBigger_ShouldUpdateOldHistoryCollectionCorrectly()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
            var firebaseUser = new FirebaseUser(firebaseRef, null, null);
            var todayTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.Date);
            var plantName = "my plant name1";
            var plant = new PlantDomain
            {
                Id = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                Name = plantName,
                WateringStart = DateTimeOffset.UtcNow.AddMonths(-1).AddDays(-3),
                WateringPeriodicity = Periodicity.FiveDays,
                IconRef = "4"
            };
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = new[] {plant}.ToImmutableList(),
                Timezone = TimeSpan.Zero
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            var result = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            var cursor = await Repository.OldHistoryBson.FindAsync(Builders<BsonDocument>.Filter.Empty);
            var historyBson = await cursor.ToListAsync();
            var oldHistory = historyBson.Select(HistoryDomain.ReadFromBsonOldHistory).ToArray();

            // Assert
            oldHistory.Should().HaveCount(1);
            oldHistory.Should().Match(domains => domains.All(h => h.UserId == user.Id));
            oldHistory.Should().Match(domains => domains.All(h => h.PlantId == plant.Id));
            oldHistory.Should().Match(domains => domains.All(h => h.PlantIconRef== plant.IconRef));
            oldHistory.Should().Match(domains => domains.All(h => h.PlantName== plant.Name));
        }

        [Test]
        public async Task Handle_PlantHistoryContainsHistoryAlready_NewHistoryShouldBeAddedWithoutDuplicates()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
            var firebaseUser = new FirebaseUser(firebaseRef, null, null);
            var plantName = "my plant name1";
            var plant = new PlantDomain
            {
                Id = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                Name = plantName,
                WateringStart = DateTime.SpecifyKind(DateTimeOffset.UtcNow.AddDays(-4).Date, DateTimeKind.Utc),
                WateringPeriodicity = Periodicity.EachDay,
                FeedingStart = default,
                FeedingPeriodicity = Periodicity.EachDay,
                IconRef = "4",
                History = new[]
                {
                        new HistoryDomain
                        {
                            Date = DateTime.SpecifyKind(DateTimeOffset.UtcNow.AddDays(-4).Date, DateTimeKind.Utc),
                            Id = ObjectId.GenerateNewId().ToString(),
                            IsDone = false,
                            Type = HistoryType.Watering,
                            PlantIconRef = "4",
                            PlantId = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                            PlantName = plantName,
                            UserId = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString()
                        }
                    }.ToImmutableList()
            };
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = new[] { plant }.ToImmutableList(),
                Timezone = TimeSpan.Zero
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            var result = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);

            // Assert
            result.User.Plants.Single().History.Should().OnlyHaveUniqueItems(h => $"{h.Date.Seconds}{h.Date.Nanos}");
            result.User.Plants.Single().History.Should().HaveCount(5);
        }

        [Test]
        public async Task Handle_PlantHistoryContainsHistoryForYesterday_TodayHistoryMustBeSet()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
            var firebaseUser = new FirebaseUser(firebaseRef, null, null);
            var plantName = "my plant name1";
            var plant = new PlantDomain
            {
                Id = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                Name = plantName,
                WateringStart = DateTime.SpecifyKind(DateTimeOffset.UtcNow.AddDays(-2).Date, DateTimeKind.Utc),
                WateringPeriodicity = Periodicity.EachDay,
                FeedingStart = default,
                FeedingPeriodicity = Periodicity.EachDay,
                IconRef = "4",
                History = new[]
                {
                        new HistoryDomain
                        {
                            Date = DateTime.SpecifyKind(DateTimeOffset.UtcNow.AddDays(-1).Date, DateTimeKind.Utc),
                            Id = ObjectId.GenerateNewId().ToString(),
                            IsDone = false,
                            Type = HistoryType.Watering,
                            PlantIconRef = "4",
                            PlantId = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                            PlantName = plantName,
                            UserId = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString()
                        }
                    }.ToImmutableList()
            };
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = new[] { plant }.ToImmutableList(),
                Timezone = TimeSpan.Zero
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            var result = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);

            // Assert
            result.User.NowAndBeyond.First().Date.Should().Be(DateTime.SpecifyKind(DateTimeOffset.UtcNow.Date, DateTimeKind.Utc).ToTimestamp());
        }

        [Test]
        public async Task Handle_PlantHistoryIsEmpty_TodayHistoryMustBeSetOnce()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
            var firebaseUser = new FirebaseUser(firebaseRef, null, null);
            var plantName = "my plant name1";
            var plant = new PlantDomain
            {
                Id = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                Name = plantName,
                WateringStart = DateTime.SpecifyKind(DateTimeOffset.UtcNow.Date, DateTimeKind.Utc),
                WateringPeriodicity = Periodicity.EachDay,
                FeedingStart = default,
                FeedingPeriodicity = Periodicity.EachDay,
                IconRef = "4",
                History = ImmutableList<HistoryDomain>.Empty
            };
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = new[] { plant }.ToImmutableList(),
                Timezone = TimeSpan.Zero
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            var result = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);

            // Assert
            result.User.NowAndBeyond.Where(t => t.Date == DateTime.SpecifyKind(DateTimeOffset.UtcNow.Date, DateTimeKind.Utc).ToTimestamp()).Should().HaveCount(1);
        }

        [Test]
        public async Task Handle_UserTimezoneHasChanged_ShouldUpdateTimezoneInDb()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
            var firebaseUser = new FirebaseUser(firebaseRef, null, "3:00:00.000000");
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = ImmutableList<PlantDomain>.Empty,
                Timezone = TimeSpan.Zero
            };

            await Repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);
            var result = await _command.Handle(new GetDataCommand(new GetDataRequest(), firebaseUser), default);

            // Assert
            result.User.Timezone.Seconds.Should().Be((int)TimeSpan.FromHours(3).TotalSeconds);
        }

        private bool IsObjectId(string id) => !string.IsNullOrEmpty(id) && ObjectId.TryParse(id, out var x ) && x != ObjectId.Empty;
    }
}