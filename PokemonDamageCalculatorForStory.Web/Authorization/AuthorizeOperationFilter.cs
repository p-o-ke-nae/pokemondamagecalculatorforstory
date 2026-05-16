using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using PokemonDamageCalculatorForStory.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PokemonDamageCalculatorForStory.Authorization;

public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAllowAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (hasAllowAnonymous)
        {
            return;
        }

        var hasAuthorize = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Any()
            || context.MethodInfo.DeclaringType?
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .Any() == true;

        if (!hasAuthorize)
        {
            return;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = GoogleAuthenticationDefaults.AuthenticationScheme
                }
            }] = Array.Empty<string>()
        });

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Google access token が必要です。" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "所有者または十分なアプリケーション権限が必要です。" });
    }
}