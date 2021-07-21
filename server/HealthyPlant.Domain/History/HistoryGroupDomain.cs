using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HealthyPlant.Domain.Infrastructure;
using HealthyPlant.Domain.Plants;

namespace HealthyPlant.Domain.History
{
    public record HistoryGroupDomain
    {
        public HistoryGroupDomain(
            DateTimeOffset date = default,
            ImmutableList<HistoryDomain>? wateringHistory = null,
            ImmutableList<HistoryDomain>? mistingHistory = null,
            ImmutableList<HistoryDomain>? feedingHistory = null,
            ImmutableList<HistoryDomain>? repottingHistory = null)
        {
            Date = date == default ? DateTimeOffset.UnixEpoch : date;
            WateringHistory = wateringHistory ?? ImmutableList<HistoryDomain>.Empty;
            MistingHistory = mistingHistory ?? ImmutableList<HistoryDomain>.Empty;
            FeedingHistory = feedingHistory ?? ImmutableList<HistoryDomain>.Empty;
            RepottingHistory = repottingHistory ?? ImmutableList<HistoryDomain>.Empty;
        }

        public DateTimeOffset Date { get; init; }
        public ImmutableList<HistoryDomain> WateringHistory { get; init; }
        public ImmutableList<HistoryDomain> MistingHistory { get; init; }
        public ImmutableList<HistoryDomain> FeedingHistory { get; init; }
        public ImmutableList<HistoryDomain> RepottingHistory { get; init; }

        public static ImmutableList<HistoryGroupDomain> GetHistoryGroups(IReadOnlyList<PlantDomain> plants, DateTimeOffset start, DateTimeOffset end)
        {
            var result = new HashSet<HistoryGroupDomain>(DateEqualComparer);
            foreach (var plant in plants)
            {
                if (plant.WateringStart != DateTimeOffset.UnixEpoch && plant.WateringPeriodicity != Periodicity.None)
                {
                    var plantStart = plant.WateringStart;
                    var plantPeriod = plant.WateringPeriodicity;
                    var type = HistoryType.Watering;
                    plantStart = DateService.ShiftPlantStartToFromDate(plantPeriod, start, plantStart);
                    while (plantStart < start)
                    {
                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }

                    while (plantStart < end)
                    {
                        if (result.TryGetValue(new HistoryGroupDomain(date: plantStart), out var historyGroup))
                        {
                            result.Remove(historyGroup);
                            result.Add(historyGroup with { WateringHistory = historyGroup.WateringHistory.Add(new HistoryDomain
                            {
                                Date = plantStart,
                                IsDone = false,
                                PlantIconRef = plant.IconRef,
                                PlantId = plant.Id,
                                PlantName = plant.Name,
                                Type = type
                            })});
                        }
                        else
                        {
                            result.Add(new HistoryGroupDomain
                            {
                                Date = plantStart,
                                WateringHistory = ImmutableList.Create(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })
                            });
                        }

                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }
                }

                if (plant.MistingStart != DateTimeOffset.UnixEpoch && plant.MistingPeriodicity != Periodicity.None)
                {
                    var plantStart = plant.MistingStart;
                    var plantPeriod = plant.MistingPeriodicity;
                    var type = HistoryType.Misting;
                    plantStart = DateService.ShiftPlantStartToFromDate(plantPeriod, start, plantStart);
                    while (plantStart < start)
                    {
                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }

                    while (plantStart < end)
                    {
                        if (result.TryGetValue(new HistoryGroupDomain(date: plantStart), out var historyGroup))
                        {
                            result.Remove(historyGroup);
                            result.Add(historyGroup with {
                                MistingHistory = historyGroup.MistingHistory.Add(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })});
                        }
                        else
                        {
                            result.Add(new HistoryGroupDomain
                            {
                                Date = plantStart,
                                MistingHistory = ImmutableList.Create(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })
                            });
                        }

                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }
                }

                if (plant.FeedingStart != DateTimeOffset.UnixEpoch && plant.FeedingPeriodicity != Periodicity.None)
                {
                    var plantStart = plant.FeedingStart;
                    var plantPeriod = plant.FeedingPeriodicity;
                    var type = HistoryType.Feeding;
                    plantStart = DateService.ShiftPlantStartToFromDate(plantPeriod, start, plantStart);
                    while (plantStart < start)
                    {
                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }

                    while (plantStart < end)
                    {
                        if (result.TryGetValue(new HistoryGroupDomain(date: plantStart), out var historyGroup))
                        {
                            result.Remove(historyGroup);
                            result.Add(historyGroup with
                                {
                                FeedingHistory = historyGroup.FeedingHistory.Add(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })
                                });
                        }
                        else
                        {
                            result.Add(new HistoryGroupDomain
                            {
                                Date = plantStart,
                                FeedingHistory = ImmutableList.Create(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })
                            });
                        }

                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }
                }

                if (plant.RepottingStart != DateTimeOffset.UnixEpoch && plant.RepottingPeriodicity != Periodicity.None)
                {
                    var plantStart = plant.RepottingStart;
                    var plantPeriod = plant.RepottingPeriodicity;
                    var type = HistoryType.Repotting;
                    plantStart = DateService.ShiftPlantStartToFromDate(plantPeriod, start, plantStart);
                    while (plantStart < start)
                    {
                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }

                    while (plantStart < end)
                    {
                        if (result.TryGetValue(new HistoryGroupDomain(date: plantStart), out var historyGroup))
                        {
                            result.Remove(historyGroup);
                            result.Add(historyGroup with
                                {
                                RepottingHistory = historyGroup.RepottingHistory.Add(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })
                                });
                        }
                        else
                        {
                            result.Add(new HistoryGroupDomain
                            {
                                Date = plantStart,
                                RepottingHistory = ImmutableList.Create(new HistoryDomain
                                {
                                    Date = plantStart,
                                    IsDone = false,
                                    PlantIconRef = plant.IconRef,
                                    PlantId = plant.Id,
                                    PlantName = plant.Name,
                                    Type = type
                                })
                            });
                        }

                        plantStart = DateService.ShiftDateToPeriodicity(plantPeriod, plantStart);
                    }
                }
            }

            return result.ToImmutableList();
        }

        public static HistoryGroupDomain? GetHistoryGroupForUserToday(ImmutableList<PlantDomain> plants, DateTimeOffset date)
        {
            var histories = new List<HistoryDomain>();
            foreach (var p in plants)
            foreach (var history in p.History)
            {
                if (history.Date == date)
                {
                    var h = history with { PlantIconRef = p.IconRef, PlantId = p.Id, PlantName = p.Name };
                    histories.Add(h);
                }
            }

            var todayHistory = histories.ToArray();
            if (todayHistory.Length == 0)
            {
                return null;
            }

            return new HistoryGroupDomain
            {
                Date = date,
                WateringHistory = todayHistory.Where(t => t.Type == HistoryType.Watering).ToImmutableList(),
                MistingHistory = todayHistory.Where(t => t.Type == HistoryType.Misting).ToImmutableList(),
                FeedingHistory = todayHistory.Where(t => t.Type == HistoryType.Feeding).ToImmutableList(),
                RepottingHistory = todayHistory.Where(t => t.Type == HistoryType.Repotting).ToImmutableList()
            };
        }

        public static IComparer<HistoryGroupDomain> DateSortAscendingComparer { get; } = new DateComparer();

        public static IEqualityComparer<HistoryGroupDomain> DateEqualComparer { get; } = new DateEqualityComparer();

        private sealed class DateEqualityComparer : IEqualityComparer<HistoryGroupDomain>
        {
            public bool Equals(HistoryGroupDomain? x, HistoryGroupDomain? y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Date.Equals(y.Date);
            }

            public int GetHashCode(HistoryGroupDomain obj)
            {
                return obj.Date.GetHashCode();
            }
        }

        private sealed class DateComparer : IComparer<HistoryGroupDomain>
        {
            public int Compare(HistoryGroupDomain? x, HistoryGroupDomain? y)
            {
                if (DateEqualComparer.Equals(x, y)) return 0;
                if (x?.Date < y?.Date) return -1;

                return 1;
            }
        }
    }
}