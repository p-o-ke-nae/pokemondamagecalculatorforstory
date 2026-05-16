using MediatR;
using PokenaeTemplate.Domain.Ports;

namespace PokenaeTemplate.Application.UseCases.Commands;

public class DeleteWeatherForecastCommandHandler : IRequestHandler<DeleteWeatherForecastCommand, DeleteWeatherForecastResponse>
{
    private readonly IWeatherForecastRepository _repository;

    public DeleteWeatherForecastCommandHandler(IWeatherForecastRepository repository)
    {
        _repository = repository;
    }

    public async Task<DeleteWeatherForecastResponse> Handle(DeleteWeatherForecastCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(request.Id, cancellationToken);

        return new DeleteWeatherForecastResponse
        {
            Success = deleted,
            Message = deleted ? "Weather forecast deleted successfully." : "Weather forecast not found."
        };
    }
}