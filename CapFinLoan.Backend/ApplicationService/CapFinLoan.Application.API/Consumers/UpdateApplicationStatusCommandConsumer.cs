using CapFinLoan.Application.Application.DTOs;
using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Messaging.Contracts.ApplicationStatus;
using MassTransit;

namespace CapFinLoan.Application.API.Consumers;

public class UpdateApplicationStatusCommandConsumer : IConsumer<UpdateApplicationStatusCommand>
{
    private readonly ILoanApplicationService _loanApplicationService;
    private readonly ILogger<UpdateApplicationStatusCommandConsumer> _logger;

    public UpdateApplicationStatusCommandConsumer(
        ILoanApplicationService loanApplicationService,
        ILogger<UpdateApplicationStatusCommandConsumer> logger)
    {
        _loanApplicationService = loanApplicationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UpdateApplicationStatusCommand> context)
    {
        var message = context.Message;

        try
        {
            var request = new UpdateApplicationStatusInternalRequestDto
            {
                Status = message.Status,
                Remarks = message.Remarks
            };

            var result = await _loanApplicationService.UpdateApplicationStatusInternalAsync(message.ApplicationId, request);

            _logger.LogInformation(
                "Processed UpdateApplicationStatusCommand for application {ApplicationId} to status {Status}",
                message.ApplicationId,
                message.Status);

            await context.RespondAsync(new UpdateApplicationStatusResult
            {
                Success = true,
                Message = string.IsNullOrWhiteSpace(result.Message)
                    ? "Application status updated successfully."
                    : result.Message,
                UpdatedStatus = result.Status
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex,
                "Application status update command rejected for ApplicationId {ApplicationId} from {Source}",
                message.ApplicationId,
                message.Source);

            await context.RespondAsync(new UpdateApplicationStatusResult
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Application status transition invalid for ApplicationId {ApplicationId} from {Source}",
                message.ApplicationId,
                message.Source);

            await context.RespondAsync(new UpdateApplicationStatusResult
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}
