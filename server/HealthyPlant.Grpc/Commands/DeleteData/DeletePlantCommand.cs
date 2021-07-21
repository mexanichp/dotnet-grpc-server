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

namespace HealthyPlant.Grpc.Commands.DeleteData
{
    public class DeletePlantCommand : IRequest<DeletePlantResponse>
    {
        public DeletePlantRequest Proto { get; }
        public FirebaseUser User { get; }

        public DeletePlantCommand(DeletePlantRequest proto, FirebaseUser user)
        {
            Proto = proto ?? throw new ArgumentNullException(nameof(proto));
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    public class DeletePlantCommandHandler : IRequestHandler<DeletePlantCommand, DeletePlantResponse>
    {
        private readonly IMongoRepository _repository;

        public DeletePlantCommandHandler(IMongoRepository repository)
        {
            _repository = repository;
        }

        public async Task<DeletePlantResponse> Handle(DeletePlantCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.User.Uid) || string.IsNullOrEmpty(request.Proto?.Id) || !ObjectId.TryParse(request.Proto.Id, out var objectId))
            {
                return new DeletePlantResponse
                {
                    ErrorCode = ErrorCode.Validation
                };
            }

            var userDomain = new UserDomain(firebaseRef: request.User.Uid, email: request.User.Email, timezone: request.User.TimezoneOffset);
            var userCursor = await _repository.UsersBson.FindAsync(userDomain.GetFirebaseRefQueryBuilder(), null, cancellationToken);
            var userBson = await userCursor.FirstOrDefaultAsync(cancellationToken);
            if (userBson == null)
            {
                return new DeletePlantResponse { ErrorCode = ErrorCode.UserNotFound };
            }

            userDomain = UserDomain.ReadFromBson(userBson) with { FirebaseRef = userDomain.FirebaseRef, Email = userDomain.Email, Timezone = userDomain.Timezone };
            userDomain = userDomain.RemovePlant(request.Proto.Id);

            await _repository.UsersBson.ReplaceOneAsync(userDomain.GetFirebaseRefQueryBuilder(), userDomain.WriteToBson(), null as ReplaceOptions, cancellationToken);

            return new DeletePlantResponse
            {
                User = userDomain.Adapt<User>()
            };
        }
    }
}