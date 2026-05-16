using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PokemonDamageCalculatorForStory.Infrastructure.Data;

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