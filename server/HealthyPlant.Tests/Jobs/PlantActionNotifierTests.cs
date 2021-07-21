using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using FluentAssertions;
using HealthyPlant.Data;
using HealthyPlant.Domain.History;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Jobs;
using HealthyPlant.Tests.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NUnit.Framework;

namespace HealthyPlant.Tests.Jobs
{
    public class PlantActionNotifierTests
    {
        private Mock<IServiceProvider> _serviceProvider;
        private MongoFixture _mongo;
        private IMongoRepository _repository;
        private IAppFirebaseMessaging _appFirebaseMessaging;

        [SetUp]
        public void SetUp()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IConfiguration)))
                .Returns(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "NOTIFICATION_INTERVAL_MINUTES", "20" }
                }).Build());
            
            _appFirebaseMessaging = Mock.Of<IAppFirebaseMessaging>();
            _serviceProvider
                .Setup(x => x.GetService(typeof(IAppFirebaseMessaging)))
                .Returns(_appFirebaseMessaging);

            _mongo = new MongoFixture();
            _repository = _mongo.Repository;

            _serviceProvider
                .Setup(x => x.GetService(typeof(IMongoRepository)))
                .Returns(_repository);

            var serviceScope = new Mock<IServiceScope>();
            serviceScope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);

            var serviceScopeFactory = new Mock<IServiceScopeFactory>();
            serviceScopeFactory
                .Setup(x => x.CreateScope())
                .Returns(serviceScope.Object);

            _serviceProvider
                .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(serviceScopeFactory.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _mongo?.Dispose();
        }

        [Test]
        public async Task NotifyAsync_Invoked_ShouldSetCorrectNotificationDate()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
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
                }.ToImmutableList(),
            };
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = new[] { plant }.ToImmutableList(),
                Timezone = TimeSpan.FromHours(2),
                NotificationTime = TimeSpan.FromHours(DateTime.UtcNow.Hour + 2),
                LastNotificationTime = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-1).AddHours(DateTime.UtcNow.Hour + 2 + 4), TimeSpan.Zero),
                FirebaseRegistrationTokens = ImmutableList.Create(Guid.NewGuid().ToString())
            };

            await _repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            using var job = new PlantActionNotifier(_serviceProvider.Object, NullLogger<PlantActionNotifier>.Instance);
            await job.NotifyAsync(CancellationToken.None);

            // Assert
            var cursor = await _repository.UsersBson.FindAsync<BsonDocument>(user.GetFirebaseRefQueryBuilder());
            var bsonUser = await cursor.FirstOrDefaultAsync();
            user = UserDomain.ReadFromBson(bsonUser);
            user.LastNotificationTime.Should()
                .Be(new DateTimeOffset(DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour), TimeSpan.Zero));

            Mock.Get(_appFirebaseMessaging)
                .Verify(t => t.SendMulticastAsync(It.IsAny<MulticastMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task NotifyAsync_NoHistoryToday_ShouldNotSendNotifications()
        {
            // Arrange
            var firebaseRef = "6E339DF8-A5EC-4F38-8DC2-4DD8D149D11D";
            var plantName = "my plant name1";
            var plant = new PlantDomain
            {
                Id = ObjectId.Parse("5ff5ba352795f8112cd8c362").ToString(),
                Name = plantName,
                WateringStart = DateTime.SpecifyKind(DateTimeOffset.UtcNow.AddDays(-4).Date, DateTimeKind.Utc),
                WateringPeriodicity = Periodicity.EachWeek,
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
                }.ToImmutableList(),
            };
            var lastNotificationTime = new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-4).AddHours(DateTime.UtcNow.Hour + 2 + 4), TimeSpan.Zero);
            var user = new UserDomain
            {
                DateFormat = "yyyy-MM-dd",
                FirebaseRef = firebaseRef,
                Email = "test2@mail.com",
                Id = ObjectId.Parse("5ff5ba352795f8111cd8c362").ToString(),
                Plants = new[] { plant }.ToImmutableList(),
                Timezone = TimeSpan.FromHours(2),
                NotificationTime = TimeSpan.FromHours(DateTime.UtcNow.Hour + 2),
                LastNotificationTime = lastNotificationTime,
                FirebaseRegistrationTokens = ImmutableList.Create(Guid.NewGuid().ToString())
            };

            await _repository.UsersBson.InsertOneAsync(user.WriteToBson());

            // Act
            using var job = new PlantActionNotifier(_serviceProvider.Object, NullLogger<PlantActionNotifier>.Instance);
            await job.NotifyAsync(CancellationToken.None);

            // Assert
            var cursor = await _repository.UsersBson.FindAsync<BsonDocument>(user.GetFirebaseRefQueryBuilder());
            var bsonUser = await cursor.FirstOrDefaultAsync();
            user = UserDomain.ReadFromBson(bsonUser);
            user.LastNotificationTime.Should().Be(lastNotificationTime);

            Mock.Get(_appFirebaseMessaging)
                .Verify(t => t.SendMulticastAsync(It.IsAny<MulticastMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}