using System;
using System.Collections.Immutable;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using HealthyPlant.Domain.History;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using Mapster;
using MongoDB.Bson;

namespace HealthyPlant.Grpc.Infrastructure
{
    public static class MappingConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig.GlobalSettings.Default
                .AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);

            MapHistoryType();

            MapPlant();

            MapUser();

            MapHistory();

            MapHistoryGroup();
        }

        private static void MapHistoryType()
        {
            TypeAdapterConfig<HistoryType, Domain.History.HistoryType>
                .NewConfig()
                .MapWith(type => HistoryTypeToHistoryTypeDb(type));

            TypeAdapterConfig<Domain.History.HistoryType, HistoryType>
                .NewConfig()
                .MapWith(type => HistoryTypeDbToHistoryType(type));
        }

        private static Domain.History.HistoryType HistoryTypeToHistoryTypeDb(HistoryType type) => type switch
        {
            HistoryType.Watering => Domain.History.HistoryType.Watering,
            HistoryType.Feeding => Domain.History.HistoryType.Feeding,
            HistoryType.Misting => Domain.History.HistoryType.Misting,
            HistoryType.Repotting => Domain.History.HistoryType.Repotting,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        private static HistoryType HistoryTypeDbToHistoryType(Domain.History.HistoryType type) => type switch
        {
            Domain.History.HistoryType.Watering => HistoryType.Watering,
            Domain.History.HistoryType.Feeding => HistoryType.Feeding,
            Domain.History.HistoryType.Misting => HistoryType.Misting,
            Domain.History.HistoryType.Repotting => HistoryType.Repotting,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        private static void MapUser()
        {
            TypeAdapterConfig<UserDomain, User>
                .NewConfig()
                .Map(dest => dest.Timezone, db => Duration.FromTimeSpan(db.Timezone))
                .Map(dest => dest.NotificationTime, db => Duration.FromTimeSpan(db.NotificationTime))
                .Ignore(nameof(UserDomain.NowAndBeyond), nameof(UserDomain.Plants))
                .AfterMappingInline((domain, user) => user.Plants.AddRange(domain.Plants.Select(p => p.Adapt<Plant>())))
                .AfterMappingInline((domain, user) => user.NowAndBeyond.AddRange(domain.NowAndBeyond.Value.Select(p => p.Adapt<HistoryGroup>())));
        }

        private static void MapPlant()
        {
            TypeAdapterConfig<PlantDomain, Plant>
                .NewConfig()
                .Map(dest => dest.FeedingStart, src => src.FeedingStart.ToTimestamp())
                .Map(dest => dest.WateringStart, src => src.WateringStart.ToTimestamp())
                .Map(dest => dest.MistingStart, src => src.MistingStart.ToTimestamp())
                .Map(dest => dest.RepottingStart, src => src.RepottingStart.ToTimestamp())
                .Map(dest => dest.WateringDays, src => MapPeriodicity(src.WateringPeriodicity))
                .Map(dest => dest.MistingDays, src => MapPeriodicity(src.MistingPeriodicity))
                .Map(dest => dest.FeedingDays, src => MapPeriodicity(src.FeedingPeriodicity))
                .Map(dest => dest.RepottingDays, src => MapPeriodicity(src.RepottingPeriodicity))
                .Map(dest => dest.WateringNext, src => src.WateringNext.Value.ToTimestamp())
                .Map(dest => dest.MistingNext, src => src.MistingNext.Value.ToTimestamp())
                .Map(dest => dest.FeedingNext, src => src.FeedingNext.Value.ToTimestamp())
                .Map(dest => dest.RepottingNext, src => src.RepottingNext.Value.ToTimestamp())
                .AfterMappingInline((domain, plant) => plant.History.AddRange(domain.History.Select(h => h.Adapt<History>())));

            TypeAdapterConfig<Plant, PlantDomain>
                .NewConfig()
                .ConstructUsing(plant => new PlantDomain
                (
                    plant.Id,
                    plant.Name,
                    plant.Notes,
                    plant.IconRef,
                    plant.WateringStart.Seconds,
                    (ushort) MapPeriodicity(plant.WateringDays),
                    plant.MistingStart.Seconds,
                    (ushort) MapPeriodicity(plant.MistingDays),
                    plant.FeedingStart.Seconds,
                    (ushort) MapPeriodicity(plant.FeedingDays),
                    plant.RepottingStart.Seconds,
                    (ushort) MapPeriodicity(plant.RepottingDays),
                    plant.History.Select(t => t.Adapt<HistoryDomain>()).ToImmutableList(),
                    default
                ))
                .IgnoreNonMapped(true);
        }

        private static void MapHistory()
        {
            TypeAdapterConfig<HistoryDomain, History>
                .NewConfig()
                .Map(dest => dest.Date, src => src.Date.ToTimestamp())
                .Map(dest => dest.PlantRef, src => src.PlantId);

            TypeAdapterConfig<History, HistoryDomain>
                .NewConfig()
                .ConstructUsing(history => new HistoryDomain
                (
                    history.Id,
                    history.Date.Seconds,
                    history.IsDone,
                    history.Type.Adapt<Domain.History.HistoryType>(),
                    null,
                    null,
                    null,
                    null
                ))
                .IgnoreNonMapped(true);
        }

        private static void MapHistoryGroup()
        {
            TypeAdapterConfig<HistoryGroupDomain, HistoryGroup>
                .NewConfig()
                .Map(dest => dest.Date, src => src.Date.ToTimestamp())
                .IgnoreNonMapped(true)
                .AfterMappingInline((domain, group) => group.WateringHistory.AddRange(domain.WateringHistory.Select(t => t.Adapt<History>())))
                .AfterMappingInline((domain, group) => group.MistingHistory.AddRange(domain.MistingHistory.Select(t => t.Adapt<History>())))
                .AfterMappingInline((domain, group) => group.FeedingHistory.AddRange(domain.FeedingHistory.Select(t => t.Adapt<History>())))
                .AfterMappingInline((domain, group) => group.RepottingHistory.AddRange(domain.RepottingHistory.Select(t => t.Adapt<History>())));
        }

        private static bool IsObjectId(string id) => !string.IsNullOrEmpty(id) && ObjectId.TryParse(id, out _);

        private static Periodicity MapPeriodicity(Domain.Plants.Periodicity periodicity) => periodicity switch
        {
            Domain.Plants.Periodicity.None => Periodicity.None,
            Domain.Plants.Periodicity.EachDay => Periodicity.EachDay,
            Domain.Plants.Periodicity.TwoDays => Periodicity.TwoDays,
            Domain.Plants.Periodicity.ThreeDays => Periodicity.ThreeDays,
            Domain.Plants.Periodicity.FourDays => Periodicity.FourDays,
            Domain.Plants.Periodicity.FiveDays => Periodicity.FiveDays,
            Domain.Plants.Periodicity.SixDays => Periodicity.SixDays,
            Domain.Plants.Periodicity.EachWeek => Periodicity.EachWeek,
            Domain.Plants.Periodicity.TenDays => Periodicity.TenDays,
            Domain.Plants.Periodicity.TwoWeeks => Periodicity.TwoWeeks,
            Domain.Plants.Periodicity.ThreeWeeks => Periodicity.ThreeWeeks,
            Domain.Plants.Periodicity.EachMonth => Periodicity.EachMonth,
            Domain.Plants.Periodicity.SixMonths => Periodicity.SixMonths,
            Domain.Plants.Periodicity.EachYear => Periodicity.EachYear,
            Domain.Plants.Periodicity.TwoYears => Periodicity.TwoYears,
            _ => throw new ArgumentOutOfRangeException(nameof(periodicity), periodicity, null)
        };

        private static Domain.Plants.Periodicity MapPeriodicity(Periodicity periodicity) => periodicity switch
        {
            Periodicity.None => Domain.Plants.Periodicity.None,
            Periodicity.EachDay => Domain.Plants.Periodicity.EachDay,
            Periodicity.TwoDays => Domain.Plants.Periodicity.TwoDays,
            Periodicity.ThreeDays => Domain.Plants.Periodicity.ThreeDays,
            Periodicity.FourDays => Domain.Plants.Periodicity.FourDays,
            Periodicity.FiveDays => Domain.Plants.Periodicity.FiveDays,
            Periodicity.SixDays => Domain.Plants.Periodicity.SixDays,
            Periodicity.EachWeek => Domain.Plants.Periodicity.EachWeek,
            Periodicity.TenDays => Domain.Plants.Periodicity.TenDays,
            Periodicity.TwoWeeks => Domain.Plants.Periodicity.TwoWeeks,
            Periodicity.ThreeWeeks => Domain.Plants.Periodicity.ThreeWeeks,
            Periodicity.EachMonth => Domain.Plants.Periodicity.EachMonth,
            Periodicity.SixMonths => Domain.Plants.Periodicity.SixMonths,
            Periodicity.EachYear => Domain.Plants.Periodicity.EachYear,
            Periodicity.TwoYears => Domain.Plants.Periodicity.TwoYears,
            _ => throw new ArgumentOutOfRangeException(nameof(periodicity), periodicity, null)
        };
    }
}