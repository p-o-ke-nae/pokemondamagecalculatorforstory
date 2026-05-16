using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using PokenaeTemplate.Application.Authorization;
using PokenaeTemplate.Domain.Ports;
using PokenaeTemplate.Extensions;

namespace PokenaeTemplate.Authorization;

public class WeatherForecastManagementAuthorizationHandler : AuthorizationHandler<WeatherForecastManagementRequirement>
{
    private readonly IWeatherForecastAccessEvaluator _accessEvaluator;
    private readonly IUserAuthorizationInfoRepository _userAuthorizationInfoRepository;
    private readonly IWeatherForecastRepository _weatherForecastRepository;

    public WeatherForecastManagementAuthorizationHandler(
        IWeatherForecastRepository weatherForecastRepository,
        IUserAuthorizationInfoRepository userAuthorizationInfoRepository,
        IWeatherForecastAccessEvaluator accessEvaluator)
    {
        _weatherForecastRepository = weatherForecastRepository;
        _userAuthorizationInfoRepository = userAuthorizationInfoRepository;
        _accessEvaluator = accessEvaluator;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, WeatherForecastManagementRequirement requirement)
    {
        var httpContext = resolveHttpContext(context.Resource);
        if (httpContext == null)
        {
            return;
        }

        var googleUserId = context.User.GetGoogleUserIdOrNull();
        if (string.IsNullOrWhiteSpace(googleUserId))
        {
            return;
        }

        if (!int.TryParse(httpContext.Request.RouteValues["id"]?.ToString(), out var weatherForecastId))
        {
            return;
        }

        var weatherForecast = await _weatherForecastRepository.FindByIdAsync(weatherForecastId, httpContext.RequestAborted);
        if (weatherForecast == null)
        {
            return;
        }

        var userAuthorizationInfo = await _userAuthorizationInfoRepository.FindByGoogleUserIdAsync(googleUserId, httpContext.RequestAborted);
        if (_accessEvaluator.CanManage(weatherForecast, googleUserId, userAuthorizationInfo))
        {
            context.Succeed(requirement);
        }
    }

    private static HttpContext? resolveHttpContext(object? resource)
    {
        return resource switch
        {
            HttpContext httpContext => httpContext,
            AuthorizationFilterContext filterContext => filterContext.HttpContext,
            _ => null
        };
    }
}