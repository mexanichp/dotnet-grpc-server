using System;
using System.Threading;
using System.Threading.Tasks;
using HealthyPlant.Data;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Models;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HealthyPlant.Grpc.Commands.UpdateData
{
    public class UpdateUserTokenCommand : IRequest<UpdateUserTokenResponse>
    {
        public UpdateUserTokenRequest UserToken { get; }
        public FirebaseUser User { get; }

        public UpdateUserTokenCommand(UpdateUserTokenRequest userToken, FirebaseUser user)
        {
            UserToken = userToken ?? throw new ArgumentNullException(nameof(userToken));
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    public class  UpdateUserTokenHandler : IRequestHandler<UpdateUserTokenCommand, UpdateUserTokenResponse>
    {
        private readonly IMongoRepository _repository;

        public UpdateUserTokenHandler(IMongoRepository repository)
        {
            _repository = repository;
        }

        public async Task<UpdateUserTokenResponse> Handle(UpdateUserTokenCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request?.UserToken?.Token))
            {
                return new UpdateUserTokenResponse {ErrorCode = ErrorCode.Validation};
            }

            var user = new UserDomain(firebaseRef: request.User.Uid);
            var u = Builders<BsonDocument>.Update.AddToSet(UserDomain.FirebaseRegistrationTokensField, request.UserToken.Token.Trim());
            var result = await _repository.UsersBson.UpdateOneAsync(user.GetFirebaseRefQueryBuilder(), u, null, cancellationToken);

            if (result.MatchedCount == 0)
            {
                return new UpdateUserTokenResponse {ErrorCode = ErrorCode.DocumentNotFound};
            }

            return new UpdateUserTokenResponse();
        }
    }
}