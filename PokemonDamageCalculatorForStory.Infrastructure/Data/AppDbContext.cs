using Microsoft.EntityFrameworkCore;
using PokemonDamageCalculatorForStory.Infrastructure.Data.Models;

namespace PokemonDamageCalculatorForStory.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<PersistedRuleSet> PersistedRuleSets => Set<PersistedRuleSet>();
    public DbSet<PersistedRun> PersistedRuns => Set<PersistedRun>();
    public DbSet<PersistedBattle> PersistedBattles => Set<PersistedBattle>();
    public DbSet<PersistedOwnPokemonSnapshot> PersistedOwnPokemonSnapshots => Set<PersistedOwnPokemonSnapshot>();
    public DbSet<PersistedCalculationResult> PersistedCalculationResults => Set<PersistedCalculationResult>();
    public DbSet<PersistedUserAuthorizationInfo> PersistedUserAuthorizationInfos => Set<PersistedUserAuthorizationInfo>();
    public DbSet<PersistedUserPermission> PersistedUserPermissions => Set<PersistedUserPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PersistedRuleSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.ToTable("RuleSets");
        });

        modelBuilder.Entity<PersistedRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.HasOne<PersistedRuleSet>()
                .WithMany()
                .HasForeignKey(r => r.RuleSetId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("Runs");
        });

        modelBuilder.Entity<PersistedBattle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EnemyPokemon).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.RunId);
            entity.HasOne<PersistedRun>()
                .WithMany()
                .HasForeignKey(b => b.RunId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable("Battles");
        });

        modelBuilder.Entity<PersistedOwnPokemonSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Species).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BaseStats).HasMaxLength(500).IsRequired();
            entity.Property(e => e.IVs).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Stats).HasMaxLength(500).IsRequired();
            entity.Property(e => e.EVs).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => e.BattleId);
            entity.HasOne<PersistedBattle>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.BattleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PersistedRun>()
                .WithMany()
                .HasForeignKey(snapshot => snapshot.RunId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("OwnPokemonSnapshots");
        });

        modelBuilder.Entity<PersistedCalculationResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AttackerParams).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.DefenderParams).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.DamageRollsJson).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => e.RunId);
            entity.HasIndex(e => e.BattleId);
            entity.HasOne<PersistedBattle>()
                .WithMany()
                .HasForeignKey(result => result.BattleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PersistedRun>()
                .WithMany()
                .HasForeignKey(result => result.RunId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.ToTable("CalculationResults");
        });

        modelBuilder.Entity<PersistedUserAuthorizationInfo>(entity =>
        {
            entity.HasKey(e => e.GoogleUserId);
            entity.Property(e => e.GoogleUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
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
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.GoogleUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Permission).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => new { e.GoogleUserId, e.Permission }).IsUnique();
            entity.ToTable("UserPermissions");
        });
    }
}
