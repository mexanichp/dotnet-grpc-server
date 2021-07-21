using System;
using FluentAssertions;
using HealthyPlant.Domain.Infrastructure;
using HealthyPlant.Domain.Plants;
using NUnit.Framework;

namespace HealthyPlant.Tests.Services
{
    [TestFixture]
    public class DateServiceTests
    {
        [Test]
        public void ShiftPlantStartToFromDate_Invoked_CalculatesShiftCorrectly()
        {
            // Arrange
            var period = Periodicity.TenDays;
            var plantStart = new DateTimeOffset(2020, 12, 1, 0, 0, 0, default);
            var today = new DateTimeOffset(2020, 12, 16, 0, 0, 0, default);

            // Act
            var result = DateService.ShiftPlantStartToFromDate(period, today, plantStart);

            // Assert
            result.Should().Be(new DateTimeOffset(2020, 12, 21, 0, 0, 0, default));
        }
    }
}