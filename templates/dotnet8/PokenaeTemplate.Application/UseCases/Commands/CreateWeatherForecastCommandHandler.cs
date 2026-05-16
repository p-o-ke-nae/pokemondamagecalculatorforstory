using MediatR;
using PokenaeTemplate.Application.DTOs;
using PokenaeTemplate.Application.Mappers;
using PokenaeTemplate.Domain.Ports;

namespace PokenaeTemplate.Application.UseCases.Commands;

/// <summary>
/// 天気予報作成コマンドハンドラー
/// ビジネスロジックをオーケストレート
/// </summary>
public class CreateWeatherForecastCommandHandler : IRequestHandler<CreateWeatherForecastCommand, CreateWeatherForecastResponse>
{
    private readonly IWeatherForecastRepository _repository;

    public CreateWeatherForecastCommandHandler(IWeatherForecastRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateWeatherForecastResponse> Handle(CreateWeatherForecastCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Domain Entity を生成（ビジネスルール検証含）
            var entity = Domain.Entities.WeatherForecast.Create(
                request.Date,
                request.TemperatureC,
                request.Summary,
                request.OwnerGoogleUserId,
                request.IsPublic
            );

            // リポジトリに保存
            var saved = await _repository.SaveAsync(entity, cancellationToken);

            // レスポンス DTO に変換
            var responseDto = saved.ToWeatherForecastResponseDto();

            return new CreateWeatherForecastResponse
            {
                Success = true,
                Message = "Weather forecast created successfully.",
                Data = responseDto
            };
        }
        catch (Exception ex)
        {
            return new CreateWeatherForecastResponse
            {
                Success = false,
                Message = ex.Message,
                Data = null
            };
        }
    }
}
