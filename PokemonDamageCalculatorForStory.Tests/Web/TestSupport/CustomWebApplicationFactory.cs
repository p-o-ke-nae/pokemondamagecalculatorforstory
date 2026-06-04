using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PokemonDamageCalculatorForStory.Application.Auditing;
using PokemonDamageCalculatorForStory.Application.Authorization;
using PokemonDamageCalculatorForStory.Authentication;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;
using PokemonDamageCalculatorForStory.Domain.Entities;

namespace PokemonDamageCalculatorForStory.Tests.Web.TestSupport;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly TestAdminAuditLogger _auditLogger = new();

    public static readonly Guid Gen6RuleSetId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DraftRuleSetId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid ArchivedRuleSetId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public const string OwnerGoogleUserId = "owner-1";
    public const string OtherGoogleUserId = "other-1";
    public const string AdminGoogleUserId = "admin-1";
    public const string MasterEditorGoogleUserId = "editor-1";
    public const string MemberGoogleUserId = "member-1";

    public TestAdminAuditLogger AuditLogger => _auditLogger;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IGoogleAccessTokenValidationService>();
            services.RemoveAll<IAdminAuditLogger>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("PokemonDamageCalculatorForStoryTests", _databaseRoot));

            services.AddScoped<IGoogleAccessTokenValidationService, FakeGoogleAccessTokenValidationService>();
            services.AddSingleton<IAdminAuditLogger>(_auditLogger);

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            Seed(dbContext);
            _auditLogger.Clear();
        });
    }

    private static void Seed(AppDbContext dbContext)
    {
        dbContext.PersistedRuleSets.Add(new PersistedRuleSet
        {
            Id = Gen6RuleSetId,
            Slug = "gen6-standard",
            Generation = 6,
            Title = "第6世代標準ルール",
            Version = "1.0",
            Status = "Active",
            Summary = "第3世代以降共通式（第6世代基準）"
        });
        dbContext.PersistedRuleSets.Add(new PersistedRuleSet
        {
            Id = DraftRuleSetId,
            Slug = "gen6-draft",
            Generation = 6,
            Title = "第6世代ドラフトルール",
            Version = "0.9",
            Status = RuleSetStatuses.Draft,
            Summary = "ドラフト"
        });
        dbContext.PersistedRuleSets.Add(new PersistedRuleSet
        {
            Id = ArchivedRuleSetId,
            Slug = "gen5-legacy",
            Generation = 5,
            Title = "第5世代旧ルール",
            Version = "0.8",
            Status = RuleSetStatuses.Archived,
            Summary = "アーカイブ"
        });
        dbContext.PersistedRuns.Add(new PersistedRun
        {
            Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            OwnerUserId = OwnerGoogleUserId,
            Name = "seed-run",
            RuleSetId = Gen6RuleSetId,
            Status = "Active"
        });

        dbContext.PersistedUserAuthorizationInfos.Add(
            new PersistedUserAuthorizationInfo
            {
                GoogleUserId = AdminGoogleUserId,
                Role = AppRoles.Administrator,
                Permissions = new List<PersistedUserPermission>
                {
                    new() { Permission = AppPermissions.MastersView },
                    new() { Permission = AppPermissions.ManageRuleSets },
                    new() { Permission = AppPermissions.ManageUserAuthorizations }
                }
            });
        dbContext.PersistedUserAuthorizationInfos.Add(
            new PersistedUserAuthorizationInfo
            {
                GoogleUserId = MasterEditorGoogleUserId,
                Role = AppRoles.MasterEditor,
                Permissions = new List<PersistedUserPermission>
                {
                    new() { Permission = AppPermissions.MastersView },
                    new() { Permission = AppPermissions.ManageRuleSets },
                    new() { Permission = AppPermissions.ManageUserAuthorizations }
                }
            });
        dbContext.PersistedUserAuthorizationInfos.Add(
            new PersistedUserAuthorizationInfo
            {
                GoogleUserId = MemberGoogleUserId,
                Role = AppRoles.Member,
                Permissions = new List<PersistedUserPermission>
                {
                    new() { Permission = AppPermissions.MastersView }
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
                "editor-token" => GoogleAccessTokenValidationResult.Valid(MasterEditorGoogleUserId, "editor@example.com", "Master Editor"),
                "member-token" => GoogleAccessTokenValidationResult.Valid(MemberGoogleUserId, "member@example.com", "Member User"),
                _ => GoogleAccessTokenValidationResult.Invalid("Invalid test token.")
            };

            return Task.FromResult(result);
        }
    }
}

public sealed class TestAdminAuditLogger : IAdminAuditLogger
{
    private readonly List<AdminAuditEntry> _entries = [];
    private readonly object _lock = new();

    public IReadOnlyList<AdminAuditEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList().AsReadOnly();
            }
        }
    }

    public Task LogAsync(AdminAuditEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _entries.Clear();
        }
    }
}
