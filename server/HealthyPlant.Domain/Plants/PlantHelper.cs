using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using HealthyPlant.Domain.Infrastructure;

namespace HealthyPlant.Domain.Plants
{
    public class PlantHelper
    {
        /// <summary>
        /// Calculate range based on period.
        /// </summary>
        /// <param name="start">Inclusive start.</param>
        /// <param name="end">Exclusive end.</param>
        /// <param name="periodicity">Period to be calculated.</param>
        /// <returns></returns>
        public static ImmutableList<DateTimeOffset> CalculateDateRange(DateTimeOffset start, DateTimeOffset end, Periodicity periodicity)
        {
            if (periodicity == Periodicity.None)
            {
                return ImmutableList<DateTimeOffset>.Empty;
            }

            var dates = new List<DateTimeOffset>();
            while (start < end)
            {
                dates.Add(start);

                start = DateService.ShiftDateToPeriodicity(periodicity, start);
            }

            return dates.ToImmutableList();
        }
    }
}