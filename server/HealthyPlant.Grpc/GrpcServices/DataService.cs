using System.Threading.Tasks;
using Grpc.Core;
using HealthyPlant.Grpc.Commands.CreateData;
using HealthyPlant.Grpc.Commands.DeleteData;
using HealthyPlant.Grpc.Commands.GetData;
using HealthyPlant.Grpc.Commands.UpdateData;
using HealthyPlant.Grpc.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace HealthyPlant.Grpc
{
    [Authorize]
    public class DataService : Data.DataBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DataService> _logger;

        public DataService(IMediator mediator, ILogger<DataService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<DataResponse> GetData(GetDataRequest request, ServerCallContext context)
        {
            var command = new GetDataCommand(request, context.GetFirebaseUser());
            var dataResponse = await _mediator.Send(command, context.CancellationToken);

            return dataResponse;
        }

        public override async Task<UpdateHistoryResponse> UpdateHistory(UpdateHistoryRequest request, ServerCallContext context)
        {
            var command = new UpdateHistoryCommand(request, context.GetFirebaseUser());
            var response = await _mediator.Send(command);

            return response;
        }

        public override async Task<CreatePlantResponse> CreatePlant(CreatePlantRequest request, ServerCallContext context)
        {
            var command = new CreatePlantCommand(request, context.GetFirebaseUser());
            var response = await _mediator.Send(command, context.CancellationToken);

            return response;
        }

        public override async Task<UpdatePlantResponse> UpdatePlant(UpdatePlantRequest request, ServerCallContext context)
        {
            var command = new UpdatePlantCommand(request, context.GetFirebaseUser());
            var response = await _mediator.Send(command, context.CancellationToken);

            return response;
        }

        public override async Task<DeletePlantResponse> DeletePlant(DeletePlantRequest request, ServerCallContext context)
        {
            var command = new DeletePlantCommand(request, context.GetFirebaseUser());
            var response = await _mediator.Send(command, context.CancellationToken);

            return response;
        }

        public override async Task<UpdateUserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
        {
            var command = new UpdateUserCommand(request, context.GetFirebaseUser());
            var response = await _mediator.Send(command, context.CancellationToken);

            return response;
        }

        public override async Task<UpdateUserTokenResponse> UpdateUserToken(UpdateUserTokenRequest request, ServerCallContext context)
        {
            var command = new UpdateUserTokenCommand(request, context.GetFirebaseUser());
            var response = await _mediator.Send(command, context.CancellationToken);

            return response;
        }
    }
}