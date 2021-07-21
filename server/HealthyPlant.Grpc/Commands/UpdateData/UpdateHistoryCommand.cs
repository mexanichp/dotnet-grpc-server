using System;
using System.Threading;
using System.Threading.Tasks;
using HealthyPlant.Data;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Models;
using Mapster;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Grpc.Commands.UpdateData
{
    public class UpdateHistoryCommand : IRequest<UpdateHistoryResponse>
    {
        public UpdateHistoryRequest Proto { get; }
        public FirebaseUser User { get; }

        public UpdateHistoryCommand(UpdateHistoryRequest proto, FirebaseUser user)
        {
            Proto = proto ?? throw new ArgumentNullException(nameof(proto));
            User = user ?? throw  new ArgumentNullException(nameof(user));
        }
    }

    public class UpdateHistoryCommandHandler : IRequestHandler<UpdateHistoryCommand, UpdateHistoryResponse>
    {
        private readonly IMongoRepository _repository;

        public UpdateHistoryCommandHandler(IMongoRepository repository)
        {
            _repository = repository;
        }

        public async Task<UpdateHistoryResponse> Handle(UpdateHistoryCommand request, CancellationToken cancellationToken)
        {
            if (request.Proto.History?.Id == null || !ObjectId.TryParse(request.Proto.History.Id, out var historyId) || historyId == ObjectId.Empty)
            {
                return new UpdateHistoryResponse
                {
                    ErrorCode = ErrorCode.Validation
                };
            }
            var userDomain = new UserDomain(firebaseRef: request.User.Uid, email: request.User.Email, timezone: request.User.TimezoneOffset);
            var userCursor = await _repository.UsersBson.FindAsync(userDomain.GetFirebaseRefQueryBuilder(), null, cancellationToken);
            var userBson = await userCursor.FirstOrDefaultAsync(cancellationToken);
            if (userBson == null)
            {
                return new UpdateHistoryResponse { ErrorCode = ErrorCode.UserNotFound };
            }

            userDomain = UserDomain.ReadFromBson(userBson) with { FirebaseRef = userDomain.FirebaseRef, Email = userDomain.Email, Timezone = userDomain.Timezone };
            userDomain = userDomain.UpdateHistory(request.Proto.History.Id, request.Proto.History.IsDone);

            await _repository.UsersBson.ReplaceOneAsync(userDomain.GetFirebaseRefQueryBuilder(), userDomain.WriteToBson(), null as ReplaceOptions, cancellationToken);

            return new UpdateHistoryResponse
            {
                User = userDomain.Adapt<User>()
            };
        }
    }
}