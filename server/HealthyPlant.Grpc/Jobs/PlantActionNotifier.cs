using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using HealthyPlant.Data;
using HealthyPlant.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Grpc.Jobs
{
    public class PlantActionNotifier : IHostedService, IDisposable
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<PlantActionNotifier> _logger;

        private const long OneDayInMs = 24 * 60 * 60 * 1000;

        private const string ExprGtCurrentTimeLastNotification = @"{ $expr: { $gt: ['$current_time', '$last_notification'] } }";
        private const string UnsetCurrentTimeLastNotification = @"{ $unset: [ 'current_time', 'last_notification' ] }";

        private static readonly BsonDocument LastNotificationHours = new()
        {
            {
                "$floor",
                new BsonDocument
                {
                    {
                        "$mod", new BsonArray(new BsonValue[]
                        {
                            new BsonDocument
                            {
                                {
                                    "$divide", new BsonArray(new BsonValue[]
                                    {
                                        $"${UserDomain.LastNotificationTimeField}",
                                        60 * 60 * 1000
                                    })
                                }
                            },
                            24
                        })
                    }
                }
            }
        };

        private static BsonDocument SetCurrentTimeLastNotification => new()
        {
            {
                "$set", new BsonDocument
                {
                    {"current_time", new BsonDocument {{"$sum", new BsonArray(new BsonValue[] {DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), $"\"${UserDomain.TimezoneField}\"]"})}}},
                    {
                        "last_notification", new BsonDocument
                        {
                            {
                                "$add", new BsonArray(new BsonValue[]
                                {
                                    $"${UserDomain.LastNotificationTimeField}",
                                    new BsonDocument
                                    {
                                        {
                                            $"$multiply", new BsonArray(new BsonValue[]
                                            {
                                                $"${UserDomain.TimezoneField}",
                                                -1
                                            })
                                        }
                                    },
                                    OneDayInMs,
                                    $"${UserDomain.NotificationTimeField}",
                                    new BsonDocument
                                    {
                                        {
                                            "$multiply", new BsonArray(new BsonValue[]
                                            {
                                                -1,
                                                60,
                                                60,
                                                1000,
                                                LastNotificationHours
                                            })
                                        }
                                    }
                                })
                            }
                        }
                    }
                }
            }
        };

        private System.Timers.Timer? _timer;
        private readonly IAppFirebaseMessaging? _messaging;

        public PlantActionNotifier(IServiceProvider sp, ILogger<PlantActionNotifier> logger)
        {
            _sp = sp;
            _logger = logger;
            _messaging = sp.GetRequiredService<IAppFirebaseMessaging>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Notification scheduler has been started...");

            _ = NotifyAsync(cancellationToken);
            var utcNow = DateTime.UtcNow;
            Task.Delay(new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour + 1, 0, 5, DateTimeKind.Utc) - utcNow, cancellationToken)
                .ContinueWith(task =>
                {
                    using var scope = _sp.CreateScope();
                    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                    _ = NotifyAsync(cancellationToken);

                    var timerMinutes = config.GetValue<double>("NOTIFICATION_INTERVAL_MINUTES");
                    _timer ??= new System.Timers.Timer(TimeSpan.FromMinutes(timerMinutes).TotalMilliseconds);
                    _timer.Elapsed += async (_, _) => await NotifyAsync(cancellationToken);
                    _timer.AutoReset = true;
                    _timer.Start();
                }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Notification scheduler has been stopped...");

            _timer?.Stop();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public async Task NotifyAsync(CancellationToken cancellationToken = default)
        {
            if (_messaging == null)
            {
                _logger.LogError("Messaging has not been initialized.");
                return;
            }

            try
            {
                using var scope = _sp.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMongoRepository>();

                var result = await repository.UsersBson.Aggregate()
                    .AppendStage<BsonDocument>(SetCurrentTimeLastNotification)
                    .AppendStage(PipelineStageDefinitionBuilder.Match<BsonDocument>(ExprGtCurrentTimeLastNotification))
                    .AppendStage<BsonDocument>(UnsetCurrentTimeLastNotification)
                    .ToListAsync(cancellationToken);

                var users = result.Select(UserDomain.ReadFromBson)
                    .Where(t => t.FirebaseRegistrationTokens.Any() && t.IsTodayAnyHistory.Value)
                    .ToArray();

                if (users.Length == 0)
                {
                    _logger.LogInformation("No users to send notification");
                    return;
                }

                _logger.LogInformation($"Sending notification to {users.Length} user(s).");
                _ = _messaging.SendMulticastAsync(new MulticastMessage
                    {
                        Notification = new Notification
                        {
                            Body = "Please take actions on your plants!",
                            Title = "Plants care reminder"
                        },
                        Tokens = users.SelectMany(t => t.FirebaseRegistrationTokens).ToArray(),
                        Android = new AndroidConfig
                        {
                            Priority = Priority.High
                        },
                        Apns = new ApnsConfig
                        {
                            Aps = new Aps
                            {
                                ContentAvailable = true
                            }
                        }
                    }, cancellationToken)
                    .ContinueWith(antecedent =>
                    {
                        if (antecedent.Status == TaskStatus.Faulted)
                        {
                            _logger.LogError(antecedent.Exception?.GetBaseException(), $"An error occurred on plant action notifier {nameof(FirebaseMessaging.SendMulticastAsync)}.");
                        }
                    }, cancellationToken);

                var q = Builders<BsonDocument>.Filter.Empty;
                foreach (var user in users)
                {
                    q = q | user.GetFirebaseRefQueryBuilder();
                }

                var utcNow = DateTime.UtcNow;
                var update = Builders<BsonDocument>.Update.Set(UserDomain.LastNotificationTimeField, new BsonInt64(new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds()));
                await repository.UsersBson.UpdateManyAsync(q, update, null, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred on plant action notifier.");
            }
        }
    }
}