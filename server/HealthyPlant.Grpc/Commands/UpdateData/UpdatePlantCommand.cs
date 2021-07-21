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

namespace HealthyPlant.Grpc.Commands.UpdateData
{
    public class UpdatePlantCommand : IRequest<UpdatePlantResponse>
    {
        public UpdatePlantRequest Proto { get; }

        public FirebaseUser User { get; }

        public UpdatePlantCommand(UpdatePlantRequest proto, FirebaseUser user)
        {
            Proto = proto ?? throw new ArgumentNullException(nameof(proto));
            User = user ?? throw new ArgumentNullException(nameof(user));
        }
    }

    public class  UpdatePlantCommandHandler : IRequestHandler<UpdatePlantCommand, UpdatePlantResponse>
    {
        private readonly IMongoRepository _repository;

        public UpdatePlantCommandHandler(IMongoRepository repository)
        {
            _repository = repository;
        }

        public async Task<UpdatePlantResponse> Handle(UpdatePlantCommand request, CancellationToken cancellationToken)
        {
            if (request.Proto.Plant == null)
            {
                return new UpdatePlantResponse { ErrorCode = ErrorCode.DocumentNotFound };
            }

            if (!IsPlantValid(request.Proto.Plant, out var errorCode))
            {
                return new UpdatePlantResponse { ErrorCode = errorCode };
            }

            var userDomain = new UserDomain(firebaseRef: request.User.Uid, email: request.User.Email, timezone: request.User.TimezoneOffset);
            var userCursor = await _repository.UsersBson.FindAsync(userDomain.GetFirebaseRefQueryBuilder(), null, cancellationToken);
            var userBson = await userCursor.FirstOrDefaultAsync(cancellationToken);
            if (userBson == null)
            {
                return new UpdatePlantResponse { ErrorCode = ErrorCode.UserNotFound };
            }

            userDomain = UserDomain.ReadFromBson(userBson) with { FirebaseRef = userDomain.FirebaseRef, Email = userDomain.Email, Timezone = userDomain.Timezone };
            userDomain = userDomain.UpdatePlant(request.Proto.Plant.Adapt<PlantDomain>());

            await _repository.UsersBson.ReplaceOneAsync(userDomain.GetFirebaseRefQueryBuilder(), userDomain.WriteToBson(), null as ReplaceOptions, cancellationToken);

            return new UpdatePlantResponse
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
                plant.FeedingStart.GetOrNullIfDefault() != null && plant.FeedingDays == Periodicity.None
            )
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

            if (string.IsNullOrEmpty(plant.Id))
            {
                errorCode = ErrorCode.Validation;
                return true;
            }

            errorCode = ErrorCode.NoError;
            return true;
        }
    }
}