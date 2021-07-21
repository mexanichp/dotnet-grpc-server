using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using HealthyPlant.Data;
using HealthyPlant.Domain.History;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc;
using HealthyPlant.Grpc.Commands.UpdateData;
using HealthyPlant.Grpc.Models;
using HealthyPlant.Tests.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using NUnit.Framework;
using HistoryType = HealthyPlant.Domain.History.HistoryType;
using Periodicity = HealthyPlant.Domain.Plants.Periodicity;

namespace HealthyPlant.Tests.Commands
{
    [TestFixture]
    public class UpdateUserCommandTests
    {
        private UpdateUserCommandHandler _command;
        private MongoFixture _mongo;
        private IMongoRepository _repository;

        [SetUp]
        public void Setup()
        {
            _mongo = new MongoFixture();
            _repository = _mongo.Repository;
            _command = new UpdateUserCommandHandler(_repository, new NullLogger<UpdateUserCommandHandler>());
        }

        [TearDown]
        public void Teardown()
        {
            _mongo?.Dispose();
        }

        [Test]
        public async Task Handle_Invoked_ShouldUpdateUserSettings()
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

            await _repository.UsersBson.InsertOneAsync(user.WriteToBson());
            var updateUserRequest = new UpdateUserRequest
            {
                DateFormat = "MM-dd-yyyy",
                Language = "en-US",
                NotificationTime = Duration.FromTimeSpan(TimeSpan.FromHours(10)),
                Timezone = Duration.FromTimeSpan(TimeSpan.FromHours(-2)),
                FieldMask = FieldMask.FromFieldNumbers<UpdateUserRequest>(new[]
                {
                    UpdateUserRequest.DateFormatFieldNumber,
                    UpdateUserRequest.LanguageFieldNumber,
                    UpdateUserRequest.TimezoneFieldNumber,
                    UpdateUserRequest.NotificationTimeFieldNumber
                })
            };

            // Act
            var result = await _command.Handle(new UpdateUserCommand(updateUserRequest, firebaseUser), default);
            var resultDb = await _repository.UsersBson.FindAsync<BsonDocument>(user.GetFirebaseRefQueryBuilder(), null, CancellationToken.None);
            var resultUserDb = await resultDb.ToListAsync();
            var userDb = UserDomain.ReadFromBson(resultUserDb.Single());

            // Assert
            userDb.NotificationTime.Should().Be(updateUserRequest.NotificationTime.ToTimeSpan());
            userDb.DateFormat.Should().Be(updateUserRequest.DateFormat);
            userDb.Language.Should().Be(updateUserRequest.Language);
            //userDb.Timezone.Should().Be(updateUserRequest.Timezone.ToTimeSpan());
            result.User.DateFormat.Should().Be(updateUserRequest.DateFormat);
            result.User.Language.Should().Be(updateUserRequest.Language);
            result.User.NotificationTime.Should().Be(updateUserRequest.NotificationTime);
            //result.User.Timezone.Should().Be(updateUserRequest.Timezone);
        }

        [Test]
        public async Task Handle_InvalidDataIsPassed_ShouldNotUpdateInvalidData()
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
                Timezone = TimeSpan.FromHours(2),
                NotificationTime = TimeSpan.FromHours(1),
                Language = "ru"
            };

            await _repository.UsersBson.InsertOneAsync(user.WriteToBson());
            var updateUserRequest = new UpdateUserRequest
            {
                DateFormat = "asdf",
                Language = "test",
                NotificationTime = new Duration(),
                Timezone = new Duration(),
                FieldMask = FieldMask.FromFieldNumbers<UpdateUserRequest>(new []
                {
                    UpdateUserRequest.DateFormatFieldNumber,
                    UpdateUserRequest.LanguageFieldNumber
                })
            };

            // Act
            var result = await _command.Handle(new UpdateUserCommand(updateUserRequest, firebaseUser), default);
            var resultDb = await _repository.UsersBson.FindAsync<BsonDocument>(user.GetFirebaseRefQueryBuilder(), null, CancellationToken.None);
            var resultUserDb = await resultDb.ToListAsync();
            var userDb = UserDomain.ReadFromBson(resultUserDb.Single());

            // Assert
            userDb.NotificationTime.Should().Be(user.NotificationTime);
            userDb.DateFormat.Should().Be(user.DateFormat);
            userDb.Language.Should().Be(user.Language);
            userDb.Timezone.Should().Be(user.Timezone);
        }

        [Test]
        [Ignore("Timezone is not updated by user anymore.")]
        public async Task Handle_UserTriesToUpdateTimezone_ShouldUpdateCorrectly([ValueSource(nameof(UtcRange))] TimeSpan utcRange)
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
                Timezone = TimeSpan.FromHours(-9),
                NotificationTime = TimeSpan.FromHours(1),
                Language = "ru"
            };

            await _repository.UsersBson.InsertOneAsync(user.WriteToBson());
            var updateUserRequest = new UpdateUserRequest
            {
                Timezone = Duration.FromTimeSpan(utcRange),
                FieldMask = FieldMask.FromFieldNumbers<UpdateUserRequest>(new []
                {
                    UpdateUserRequest.TimezoneFieldNumber
                })
            };

            // Act
            var result = await _command.Handle(new UpdateUserCommand(updateUserRequest, firebaseUser), default);
            var resultDb = await _repository.UsersBson.FindAsync<BsonDocument>(user.GetFirebaseRefQueryBuilder(), null, CancellationToken.None);
            var resultUserDb = await resultDb.ToListAsync();
            var userDb = UserDomain.ReadFromBson(resultUserDb.Single());

            // Assert
            result.User.Timezone.Should().Be(updateUserRequest.Timezone);
            userDb.Timezone.Should().Be(updateUserRequest.Timezone.ToTimeSpan());
        }

        private static IEnumerable<TimeSpan> UtcRange
        {
            get
            {
                for (int i = -12; i < 13; i++)
                {
                    yield return TimeSpan.FromHours(i);
                }
            }
        }
    }
}