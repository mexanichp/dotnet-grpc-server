using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HealthyPlant.Data;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Models;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Grpc.Commands.GetData
{
    public class GetDataCommand : IRequest<DataResponse>
    {
        public GetDataRequest Proto { get; }
        public FirebaseUser User { get; }

        public GetDataCommand(GetDataRequest proto, FirebaseUser user)
        {
            Proto = proto ?? throw new ArgumentNullException(nameof(proto));
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    public class GetDataCommandHandler : IRequestHandler<GetDataCommand, DataResponse>
    {
        private readonly IMongoRepository _repository;
        private readonly ILogger _logger;

        public GetDataCommandHandler(IMongoRepository repository, ILogger<GetDataCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DataResponse> Handle(GetDataCommand request, CancellationToken cancellationToken)
        {
            var userDomain = new UserDomain(firebaseRef: request.User.Uid, email: request.User.Email, timezone: request.User.TimezoneOffset);

            var userCursor = await _repository.UsersBson.FindAsync(userDomain.GetFirebaseRefQueryBuilder(), null, cancellationToken);
            var userBson = await userCursor.FirstOrDefaultAsync(cancellationToken);
            if (userBson == null)
            {
                userDomain = userDomain.AddNewUser();
                await _repository.UsersBson.InsertOneAsync(userDomain.WriteToBson(), null, cancellationToken);
            }
            else
            {
                userDomain = UserDomain.ReadFromBson(userBson) with { FirebaseRef = userDomain.FirebaseRef, Email = userDomain.Email, Timezone = userDomain.Timezone };
                var userWithUpdatedHistory = userDomain.AddMissingHistory();
                if (userDomain != userWithUpdatedHistory)
                {
                    userDomain = userWithUpdatedHistory;
                    await _repository.UsersBson.ReplaceOneAsync(userDomain.GetFirebaseRefQueryBuilder(), userDomain.WriteToBson(),  null as ReplaceOptions, cancellationToken);
                    if (userDomain.TryGetOldHistory(out var oldHistory))
                    {
                        await _repository.OldHistoryBson.BulkWriteAsync(oldHistory.Select(t => new InsertOneModel<BsonDocument>(t.WriteToBsonOldHistory())), new BulkWriteOptions(), cancellationToken);
                    }
                }
            }

            return new DataResponse
            {
                User = userDomain.Adapt<User>()
            };
        }
    }
}