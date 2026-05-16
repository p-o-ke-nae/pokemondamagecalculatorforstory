using Microsoft.EntityFrameworkCore;
using PokenaeTemplate.Domain.Entities;
using PokenaeTemplate.Domain.Ports;
using PokenaeTemplate.Infrastructure.Data;
using PokenaeTemplate.Infrastructure.Mappers;

namespace PokenaeTemplate.Infrastructure.Repositories;

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