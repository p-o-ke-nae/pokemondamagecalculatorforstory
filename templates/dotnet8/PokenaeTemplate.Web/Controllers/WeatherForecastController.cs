using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using PokenaeTemplate.Application.DTOs;
using PokenaeTemplate.Application.UseCases.Commands;
using PokenaeTemplate.Application.UseCases.Queries;
using PokenaeTemplate.Authorization;
using PokenaeTemplate.Extensions;

namespace PokenaeTemplate.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// すべての天気予報を取得
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllWeatherForecastsQuery
        {
            RequestingGoogleUserId = User.GetGoogleUserIdOrNull()
        };

        var result = await mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// ID で天気予報を取得
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetWeatherForecastByIdQuery
        {
            Id = id,
            RequestingGoogleUserId = User.GetGoogleUserIdOrNull()
        };

        var result = await mediator.Send(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// 天気予報を作成
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateWeatherForecastRequest request)
    {
        var command = new CreateWeatherForecastCommand
        {
            Date = request.Date,
            TemperatureC = request.TemperatureC,
            Summary = request.Summary,
            OwnerGoogleUserId = User.GetRequiredGoogleUserId(),
            IsPublic = request.IsPublic
        };

        var result = await mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data?.Id }, result.Data);
    }

    /// <summary>
    /// 天気予報を更新
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AppPolicies.ManageWeatherForecast)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWeatherForecastRequest request)
    {
        var command = new UpdateWeatherForecastCommand
        {
            Id = id,
            Date = request.Date,
            TemperatureC = request.TemperatureC,
            Summary = request.Summary,
            IsPublic = request.IsPublic
        };

        var result = await mediator.Send(command);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// 天気予報を削除
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AppPolicies.ManageWeatherForecast)]
    public async Task<IActionResult> Delete(int id)
    {
        var command = new DeleteWeatherForecastCommand { Id = id };
        var result = await mediator.Send(command);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return NoContent();
    }
}
