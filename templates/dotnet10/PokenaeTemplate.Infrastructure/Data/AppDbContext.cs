using Microsoft.EntityFrameworkCore;
using PokenaeTemplate.Infrastructure.Data.Models;

namespace PokenaeTemplate.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PersistedWeatherForecast> PersistedWeatherForecasts => Set<PersistedWeatherForecast>();
    public DbSet<PersistedUserAuthorizationInfo> PersistedUserAuthorizationInfos => Set<PersistedUserAuthorizationInfo>();
    public DbSet<PersistedUserPermission> PersistedUserPermissions => Set<PersistedUserPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersistedWeatherForecast>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Date)
                .IsRequired();

            entity.Property(e => e.TemperatureC)
                .IsRequired();

            entity.Property(e => e.Summary)
                .HasMaxLength(100);

            entity.Property(e => e.OwnerGoogleUserId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.IsPublic)
                .IsRequired();

            entity.ToTable("WeatherForecasts");
        });

        modelBuilder.Entity<PersistedUserAuthorizationInfo>(entity =>
        {
            entity.HasKey(e => e.GoogleUserId);

            entity.Property(e => e.GoogleUserId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasMany(e => e.Permissions)
                .WithOne(permission => permission.UserAuthorizationInfo)
                .HasForeignKey(permission => permission.GoogleUserId)
                .HasPrincipalKey(user => user.GoogleUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable("UserAuthorizationInfos");
        });

        modelBuilder.Entity<PersistedUserPermission>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.GoogleUserId)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.Permission)
                .HasMaxLength(128)
                .IsRequired();

            entity.HasIndex(e => new { e.GoogleUserId, e.Permission })
                .IsUnique();

            entity.ToTable("UserPermissions");
        });
    }
}
