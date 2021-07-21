using System;
using System.Threading;
using System.Threading.Tasks;
using HealthyPlant.Data;
using HealthyPlant.Domain.Plants;
using HealthyPlant.Domain.Users;
using HealthyPlant.Grpc.Infrastructure;
using HealthyPlant.Grpc.Models;
using Mapster;
using MediatR;
using MongoDB.Driver;

namespace HealthyPlant.Grpc.Commands.CreateData
{
    public class CreatePlantCommand : IRequest<CreatePlantResponse>
    {
        public CreatePlantRequest Proto { get; }

        public FirebaseUser User { get; }

        public CreatePlantCommand(CreatePlantRequest proto, FirebaseUser user)
        {
            Proto = proto ?? throw new ArgumentNullException(nameof(proto));
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    public class CreatePlantCommandHandler : IRequestHandler<CreatePlantCommand, CreatePlantResponse>
    {
        private readonly IMongoRepository _repository;

        public CreatePlantCommandHandler(IMongoRepository repository)
        {
            _repository = repository;
        }

        public async Task<CreatePlantResponse> Handle(CreatePlantCommand request, CancellationToken cancellationToken)
        {
            if (request.Proto.Plant == null)
            {
                return new CreatePlantResponse { ErrorCode = ErrorCode.DocumentNotFound };
            }

            if (!IsPlantValid(request.Proto.Plant, out var errorCode))
            {
                return new CreatePlantResponse { ErrorCode = errorCode };
            }
            
            var userDomain = new UserDomain(firebaseRef: request.User.Uid, email: request.User.Email, timezone: request.User.TimezoneOffset);
            var userCursor = await _repository.UsersBson.FindAsync(userDomain.GetFirebaseRefQueryBuilder(), null, cancellationToken);
            var userBson = await userCursor.FirstOrDefaultAsync(cancellationToken);
            if (userBson == null)
            {
                return new CreatePlantResponse {ErrorCode = ErrorCode.UserNotFound};
            }

            userDomain = UserDomain.ReadFromBson(userBson) with { FirebaseRef = userDomain.FirebaseRef, Email = userDomain.Email, Timezone = userDomain.Timezone };
            userDomain = userDomain.AddNewPlant(request.Proto.Plant.Adapt<PlantDomain>());

            await _repository.UsersBson.ReplaceOneAsync(userDomain.GetFirebaseRefQueryBuilder(), userDomain.WriteToBson(), null as ReplaceOptions, cancellationToken);

            return new CreatePlantResponse
            {
                User = userDomain.Adapt<User>()
            };
        }

        private bool IsPlantValid(Plant plant, out ErrorCode errorCode)
        {
            if (plant.WateringStart.GetOrNullIfDefault() == null && plant.WateringDays != Periodicity.None ||
                plant.WateringStart.GetOrNullIfDefault() != null && plant.WateringDays == Periodicity.None)
            {
                errorCode = ErrorCode.WateringValidation;
                return false;
            }

            if (plant.FeedingStart.GetOrNullIfDefault() == null && plant.FeedingDays != Periodicity.None ||
                plant.FeedingStart.GetOrNullIfDefault() != null && plant.FeedingDays == Periodicity.None)
            {
                errorCode = ErrorCode.FeedingValidation;
                return false;
            }

            if (plant.MistingStart.GetOrNullIfDefault() == null && plant.MistingDays != Periodicity.None ||
                plant.MistingStart.GetOrNullIfDefault() != null && plant.MistingDays == Periodicity.None)
            {
                errorCode = ErrorCode.MistingValidation;
                return false;
            }

            if (plant.RepottingStart.GetOrNullIfDefault() == null && plant.RepottingDays != Periodicity.None ||
                plant.RepottingStart.GetOrNullIfDefault() != null && plant.RepottingDays == Periodicity.None)
            {
                errorCode = ErrorCode.RepottingValidation;
                return false;
            }

            if (string.IsNullOrEmpty(plant.Name?.Trim()))
            {
                errorCode = ErrorCode.NameEmpty;
                return false;
            }

            if (plant.Name.Length > Constants.NameMaxLength)
            {
                errorCode = ErrorCode.NameMaxLength;
                return false;
            }

            if (plant.Notes.Length > Constants.NotesMaxLength)
            {
                errorCode = ErrorCode.NotesMaxLength;
                return false;
            }

            if (string.IsNullOrEmpty(plant.IconRef?.Trim()))
            {
                errorCode = ErrorCode.IconRefEmpty;
                return true;
            }

            errorCode = ErrorCode.NoError;
            return true;
        }
    }
}