using System;
using HealthyPlant.Domain.Plants;

namespace HealthyPlant.Domain.Infrastructure
{
    public class DateService
    {
        public static DateTimeOffset ShiftDateToPeriodicity(Periodicity periodicity, DateTimeOffset date)
        {
            DateTimeOffset result;

            if (periodicity < Periodicity.EachMonth)
            {
                result = date.AddDays((int)periodicity);
            }
            else if (periodicity < Periodicity.EachYear)
            {
                result = date.AddMonths((int)periodicity % 100);
            }
            else
            {
                result = date.AddYears((int)periodicity % 1000);
            }

            return result;
        }

        public static DateTimeOffset ShiftPlantStartToFromDate(Periodicity period, DateTimeOffset start, DateTimeOffset plantStart)
        {
            if (start <= plantStart || period == 0) return plantStart;
            
            DateTimeOffset result;

            if (period < Periodicity.EachMonth)
            {
                var days = (int)start.Subtract(plantStart).TotalDays;
                var plantActionStart = (int) Math.Ceiling(days / (double) period);
                if (plantActionStart == 0)
                    result = start;
                else if (days < (int)period)
                    result = start.AddDays((int)period - days);
                else
                    result = plantStart.AddDays(plantActionStart * (int) period);
            }
            else if (period < Periodicity.EachYear)
            {
                result = plantStart.AddMonths((start.Year - plantStart.Year) * 12 + start.Month - start.Month);
            }
            else
            {
                result = plantStart.AddYears(start.Year - plantStart.Year);
            }

            return result;
        }

        public static DateTimeOffset GetNextDate(Periodicity period, DateTimeOffset start, DateTimeOffset plantStart)
        {
            if (period == Periodicity.None || start == default || plantStart == default || start == DateTimeOffset.UnixEpoch || plantStart == DateTimeOffset.UnixEpoch)
            {
                return DateTimeOffset.UnixEpoch;
            }
            
            var nextDate = plantStart;
            if (nextDate < start)
            {
                nextDate = ShiftPlantStartToFromDate(period, start, nextDate);
            }

            while (nextDate < start)
            {
                nextDate = ShiftDateToPeriodicity(period, nextDate);
            }

            return nextDate;
        }
    }
}