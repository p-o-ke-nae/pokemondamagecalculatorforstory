using MediatR;
using PokenaeTemplate.Application.Authorization;
using PokenaeTemplate.Application.DTOs;
using PokenaeTemplate.Application.Mappers;
using PokenaeTemplate.Domain.Ports;

namespace PokenaeTemplate.Application.UseCases.Queries;

/// <summary>
/// 天気予報一覧取得ハンドラー
/// </summary>
public class GetAllWeatherForecastsQueryHandler : IRequestHandler<GetAllWeatherForecastsQuery, GetAllWeatherForecastsResponse>
{
    private readonly IWeatherForecastAccessEvaluator _accessEvaluator;
    private readonly IWeatherForecastRepository _repository;
    private readonly IUserAuthorizationInfoRepository _userAuthorizationInfoRepository;

    public GetAllWeatherForecastsQueryHandler(
        IWeatherForecastRepository repository,
        IUserAuthorizationInfoRepository userAuthorizationInfoRepository,
        IWeatherForecastAccessEvaluator accessEvaluator)
    {
        _repository = repository;
        _userAuthorizationInfoRepository = userAuthorizationInfoRepository;
        _accessEvaluator = accessEvaluator;
    }

    public async Task<GetAllWeatherForecastsResponse> Handle(GetAllWeatherForecastsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var entities = await _repository.FindAllAsync(cancellationToken);
            var userAuthorizationInfo = await loadUserAuthorizationInfoAsync(request.RequestingGoogleUserId, cancellationToken);
            var dtos = entities
                .Where(entity => _accessEvaluator.CanRead(entity, request.RequestingGoogleUserId, userAuthorizationInfo))
                .Select(entity => entity.ToWeatherForecastResponseDto())
                .ToList();

            return new GetAllWeatherForecastsResponse
            {
                Success = true,
                Message = "Weather forecasts retrieved successfully.",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            return new GetAllWeatherForecastsResponse
            {
                Success = false,
                Message = ex.Message,
                Data = null
            };
        }
    }

    private async Task<PokenaeTemplate.Domain.Entities.UserAuthorizationInfo?> loadUserAuthorizationInfoAsync(string? googleUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            return null;
        }

        return await _userAuthorizationInfoRepository.FindByGoogleUserIdAsync(googleUserId, cancellationToken);
    }
}
