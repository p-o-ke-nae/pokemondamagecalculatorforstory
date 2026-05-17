using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    internal DbSet<PersistedRuleset> Rulesets => Set<PersistedRuleset>();
    internal DbSet<PersistedMasterVersionSet> MasterVersionSets => Set<PersistedMasterVersionSet>();
    internal DbSet<PersistedRun> Runs => Set<PersistedRun>();
    internal DbSet<PersistedShare> Shares => Set<PersistedShare>();
    internal DbSet<PersistedImportJob> ImportJobs => Set<PersistedImportJob>();
    public DbSet<PersistedUserAuthorizationInfo> PersistedUserAuthorizationInfos => Set<PersistedUserAuthorizationInfo>();
    public DbSet<PersistedUserPermission> PersistedUserPermissions => Set<PersistedUserPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersistedRuleset>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Slug)
                .HasMaxLength(128)
                .IsRequired();

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.ToTable("Rulesets");
        });

        modelBuilder.Entity<PersistedMasterVersionSet>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.ToTable("MasterVersionSets");
        });

        modelBuilder.Entity<PersistedRun>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OwnerUserId)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.ToTable("Runs");
        });

        modelBuilder.Entity<PersistedShare>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OwnerUserId)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.ToTable("Shares");
        });

        modelBuilder.Entity<PersistedImportJob>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SubmittedByUserId)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.ToTable("ImportJobs");
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
