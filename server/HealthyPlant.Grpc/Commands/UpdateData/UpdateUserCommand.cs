using System;
using System.Globalization;
using System.Linq;
using HealthyPlant.Data;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Models;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using MongoDB.Driver;
using Mapster;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc.Commands.UpdateData
{
    public class UpdateUserCommand : IRequest<UpdateUserResponse>
    {
        public UpdateUserRequest Proto { get; }
        public FirebaseUser User { get; }
        
        public UpdateUserCommand(UpdateUserRequest proto, FirebaseUser user)
        {
            Proto = proto ?? throw new ArgumentNullException(nameof(proto));
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    public class  UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
    {
        private readonly IMongoRepository _repository;
        private readonly ILogger<UpdateUserCommandHandler> _logger;

        public UpdateUserCommandHandler(IMongoRepository repository, ILogger<UpdateUserCommandHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            if (request.Proto.FieldMask == null || !request.Proto.FieldMask.Paths.Any())
            {
                return new UpdateUserResponse {ErrorCode = ErrorCode.Validation};
            }

            var userDomain = new UserDomain(firebaseRef: request.User.Uid, email: request.User.Email);
            var userCursor = await _repository.UsersBson.FindAsync(userDomain.GetFirebaseRefQueryBuilder(), null, cancellationToken);
            var userBson = await userCursor.FirstOrDefaultAsync(cancellationToken);
            if (userBson == null)
            {
                return new UpdateUserResponse {ErrorCode = ErrorCode.UserNotFound};
            }

            userDomain = UserDomain.ReadFromBson(userBson);

            var updateRequest = new UpdateUserRequest()
            {
                DateFormat = userDomain.DateFormat,
                Language = userDomain.Language,
                NotificationTime = userDomain.NotificationTime.ToDuration()
            };

            request.Proto.FieldMask.Merge(request.Proto, updateRequest, new FieldMask.MergeOptions {ReplaceMessageFields = true});

            userDomain = UpdateUserDomain(updateRequest, userDomain);

            await _repository.UsersBson.ReplaceOneAsync(userDomain.GetFirebaseRefQueryBuilder(), userDomain.WriteToBson(), null as ReplaceOptions, cancellationToken);

            return new UpdateUserResponse
            {
                User = userDomain.Adapt<User>()
            };
        }

        private UserDomain UpdateUserDomain(UpdateUserRequest updateRequest, UserDomain userDomain)
        {
            if (!string.IsNullOrEmpty(updateRequest.DateFormat) && DateTime.TryParseExact(DateTime.SpecifyKind(new DateTime(2020,12,21), DateTimeKind.Utc).ToString(updateRequest.DateFormat), updateRequest.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
            {
                userDomain = userDomain with
                {
                    DateFormat = updateRequest.DateFormat
                };
            }

            if (!string.IsNullOrEmpty(updateRequest.Language))
            {
                try
                {
                    var info = CultureInfo.GetCultureInfo(updateRequest.Language);
                    userDomain = userDomain with {Language = info.Name};
                }
                catch (CultureNotFoundException)
                {
                    // Don't set a language.
                }
            }

            return userDomain with { NotificationTime = updateRequest.NotificationTime.ToTimeSpan() };
        }
    }
}