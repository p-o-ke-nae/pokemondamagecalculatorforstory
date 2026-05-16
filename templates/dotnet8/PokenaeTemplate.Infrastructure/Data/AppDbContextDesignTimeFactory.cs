using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PokenaeTemplate.Infrastructure.Data;

public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionStringResolver = new DesignTimeConnectionStringResolver();

        optionsBuilder.UseSqlServer(
            connectionStringResolver.Resolve(),
            sqlServerOptions => sqlServerOptions.EnableRetryOnFailure());

        return new AppDbContext(optionsBuilder.Options);
    }
}