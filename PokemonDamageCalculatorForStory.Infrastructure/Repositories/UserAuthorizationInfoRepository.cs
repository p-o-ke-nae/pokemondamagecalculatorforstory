using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Domain.Entities;
using PokemonDamageCalculatorForStory.Domain.Ports;
using PokemonDamageCalculatorForStory.Infrastructure.Data;
using PokemonDamageCalculatorForStory.Infrastructure.Mappers;

namespace PokemonDamageCalculatorForStory.Infrastructure.Repositories;

public class UserAuthorizationInfoRepository : IUserAuthorizationInfoRepository
{
    private readonly AppDbContext _context;

    public UserAuthorizationInfoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserAuthorizationInfo>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedUserAuthorizationInfos
            .AsNoTracking()
            .Include(user => user.Permissions)
            .OrderBy(user => user.GoogleUserId)
            .ToListAsync(cancellationToken);

        return persisted.Select(user => user.ToDomainEntity()).ToList().AsReadOnly();
    }

    public async Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedUserAuthorizationInfos
            .AsNoTracking()
            .Include(user => user.Permissions)
            .FirstOrDefaultAsync(user => user.GoogleUserId == googleUserId, cancellationToken);

        return persisted?.ToDomainEntity();
    }

    public Task<bool> ExistsByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default)
        => _context.PersistedUserAuthorizationInfos.AnyAsync(user => user.GoogleUserId == googleUserId, cancellationToken);

    public Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default)
        => _context.PersistedUserAuthorizationInfos.CountAsync(user => user.Role == role, cancellationToken);

    public async Task AddAsync(UserAuthorizationInfo userAuthorizationInfo, CancellationToken cancellationToken = default)
    {
        await _context.PersistedUserAuthorizationInfos.AddAsync(userAuthorizationInfo.ToPersistedModel(), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAuthorizationInfo userAuthorizationInfo, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedUserAuthorizationInfos
            .Include(user => user.Permissions)
            .FirstAsync(user => user.GoogleUserId == userAuthorizationInfo.GoogleUserId, cancellationToken);

        persisted.Role = userAuthorizationInfo.Role;
        persisted.Permissions.Clear();

        foreach (var permission in userAuthorizationInfo.Permissions)
        {
            persisted.Permissions.Add(new Data.Models.PersistedUserPermission
            {
                GoogleUserId = userAuthorizationInfo.GoogleUserId,
                Permission = permission
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string googleUserId, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedUserAuthorizationInfos.FirstAsync(user => user.GoogleUserId == googleUserId, cancellationToken);
        _context.PersistedUserAuthorizationInfos.Remove(persisted);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
