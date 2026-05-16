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

    public async Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default)
    {
        var persisted = await _context.PersistedUserAuthorizationInfos
            .Include(user => user.Permissions)
            .FirstOrDefaultAsync(user => user.GoogleUserId == googleUserId, cancellationToken);

        return persisted?.ToDomainEntity();
    }
}