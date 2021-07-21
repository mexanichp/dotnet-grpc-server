using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using HealthyPlant.Data;
using HealthyPlant.Grpc;
using HealthyPlant.Grpc.Commands.CreateData;
using HealthyPlant.Grpc.Models;
using HealthyPlant.Tests.Configuration;
using MongoDB.Bson;
using NUnit.Framework;

namespace HealthyPlant.Tests.Commands
{
    [TestFixture]
    public class CreatePlantCommandTests
    {
        private CreatePlantCommandHandler _command;
        private MongoFixture _mongo;
        public IMongoRepository Repository { get; set; }

        [SetUp]
        public void Setup()
        {
            _mongo = new MongoFixture();
            Repository = _mongo.Repository;
            _command = new CreatePlantCommandHandler(Repository);
        }

        [TearDown]
        public void Teardown()
        {
            _mongo?.Dispose();
        }

        [Test]
        public async Task Handle_TodayHistoryIsNotEmpty_NewHistoryIsAddedWithCorrectPlantReference()
        {
            // Arrange
            var firebaseUser = new FirebaseUser(MongoFixture.UserId.ToString(), null, null);
            var todayTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.Date);
            var plant = new Plant
            {
                Id = "",
                Name = "Holly Bobby",
                Notes = "",
                IconRef = "4",
                WateringStart = new Timestamp(),
                FeedingStart = todayTimestamp,
                FeedingDays = Periodicity.EachDay,
                MistingStart = new Timestamp(),
                RepottingStart = new Timestamp()
            };

            // Act
            await _command.Handle(new CreatePlantCommand(new CreatePlantRequest { Plant = plant }, firebaseUser), default);
            plant.Name += "2";
            plant.MistingStart = todayTimestamp;
            plant.MistingDays = Periodicity.EachWeek;
            plant.RepottingStart = todayTimestamp;
            plant.RepottingDays = Periodicity.EachWeek;
            plant.WateringStart = todayTimestamp;
            plant.WateringDays = Periodicity.EachWeek;
            var response = await _command.Handle(new CreatePlantCommand(new CreatePlantRequest { Plant = plant }, firebaseUser), default);

            // Assert
           var user = response.User;

            var dbPlant1 = user.Plants.Single(t => t.Name == "Holly Bobby");
            var dbPlant2 = user.Plants.Single(t => t.Name == "Holly Bobby2");
            dbPlant1.History.Should().HaveCount(1);
            dbPlant2.History.Should().HaveCount(4);
            dbPlant2.History.Should().BeInDescendingOrder(t => t.Date);
            dbPlant2.History.Should().OnlyHaveUniqueItems(t => t.Id.ToString());
            dbPlant2.History.Should().Match(t => t.All(x => !string.IsNullOrEmpty(x.Id.ToString()) && x.Id != ObjectId.Empty.ToString()));
        }

        [Test]
        public async Task Handle_UserIdPerformsInjectionAttack_ParameterIsRejected()
        {
            // Arrange
            var firebaseUser = new FirebaseUser("{$ne:\"\"}", null, null);
            var todayTimestamp = Timestamp.FromDateTime(DateTime.UtcNow.Date);
            var plant = new Plant
            {
                Id = "",
                Name = "Holly Bobby123",
                Notes = "",
                IconRef = "4",
                WateringStart = new Timestamp(),
                FeedingStart = todayTimestamp,
                FeedingDays = Periodicity.EachDay,
                MistingStart = new Timestamp(),
                RepottingStart = new Timestamp()
            };

            // Act
            var response = await _command.Handle(new CreatePlantCommand(new CreatePlantRequest {Plant = plant}, firebaseUser), default);

            // Assert
            response.ErrorCode.Should().Be(ErrorCode.UserNotFound);
        }
    }
}