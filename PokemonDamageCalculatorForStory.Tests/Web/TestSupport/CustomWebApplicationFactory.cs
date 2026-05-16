using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Tests.Web.TestSupport;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    public const int PrivateForecastId = 1;
    public const int PublicForecastId = 2;
    public const string OwnerGoogleUserId = "owner-1";
    public const string OtherGoogleUserId = "other-1";
    public const string AdminGoogleUserId = "admin-1";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IGoogleAccessTokenValidationService>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("PokemonDamageCalculatorForStoryTests", _databaseRoot));

            services.AddScoped<IGoogleAccessTokenValidationService, FakeGoogleAccessTokenValidationService>();

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            Seed(dbContext);
        });
    }

    private static void Seed(AppDbContext dbContext)
    {
        dbContext.PersistedWeatherForecasts.AddRange(
            new PersistedWeatherForecast
            {
                Id = PrivateForecastId,
                Date = new DateOnly(2026, 3, 21),
                TemperatureC = 22,
                Summary = "Private forecast",
                OwnerGoogleUserId = OwnerGoogleUserId,
                IsPublic = false
            },
            new PersistedWeatherForecast
            {
                Id = PublicForecastId,
                Date = new DateOnly(2026, 3, 22),
                TemperatureC = 18,
                Summary = "Public forecast",
                OwnerGoogleUserId = OwnerGoogleUserId,
                IsPublic = true
            });

        dbContext.PersistedUserAuthorizationInfos.Add(
            new PersistedUserAuthorizationInfo
            {
                GoogleUserId = AdminGoogleUserId,
                Role = "Administrator",
                Permissions = new List<PersistedUserPermission>
                {
                    new()
                    {
                        Permission = "weatherforecast.manage.any"
                    },
                    new()
                    {
                        Permission = "weatherforecast.read.private"
                    }
                }
            });

        dbContext.SaveChanges();
    }

    private sealed class FakeGoogleAccessTokenValidationService : IGoogleAccessTokenValidationService
    {
        public Task<GoogleAccessTokenValidationResult> ValidateAsync(string accessToken, CancellationToken cancellationToken = default)
        {
            var result = accessToken switch
            {
                "owner-token" => GoogleAccessTokenValidationResult.Valid(OwnerGoogleUserId, "owner@example.com", "Owner User"),
                "other-token" => GoogleAccessTokenValidationResult.Valid(OtherGoogleUserId, "other@example.com", "Other User"),
                "admin-token" => GoogleAccessTokenValidationResult.Valid(AdminGoogleUserId, "admin@example.com", "Admin User"),
                _ => GoogleAccessTokenValidationResult.Invalid("Invalid test token.")
            };

            return Task.FromResult(result);
        }
    }
}