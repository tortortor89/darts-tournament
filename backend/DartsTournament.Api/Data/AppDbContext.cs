using Microsoft.EntityFrameworkCore;
using DartsTournament.Api.Models;

namespace DartsTournament.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentPlayer> TournamentPlayers => Set<TournamentPlayer>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchSession> MatchSessions => Set<MatchSession>();
    public DbSet<Throw> Throws => Set<Throw>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });

        // TournamentPlayer (composite key)
        modelBuilder.Entity<TournamentPlayer>(entity =>
        {
            entity.HasKey(tp => new { tp.TournamentId, tp.PlayerId });

            entity.HasOne(tp => tp.Tournament)
                .WithMany(t => t.TournamentPlayers)
                .HasForeignKey(tp => tp.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Player)
                .WithMany(p => p.TournamentPlayers)
                .HasForeignKey(tp => tp.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tp => tp.Group)
                .WithMany(g => g.Players)
                .HasForeignKey(tp => tp.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Group
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasOne(g => g.Tournament)
                .WithMany(t => t.Groups)
                .HasForeignKey(g => g.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Match
        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Group)
                .WithMany(g => g.Matches)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.Player1)
                .WithMany()
                .HasForeignKey(m => m.Player1Id)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.Player2)
                .WithMany()
                .HasForeignKey(m => m.Player2Id)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.Winner)
                .WithMany()
                .HasForeignKey(m => m.WinnerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // MatchSession
        modelBuilder.Entity<MatchSession>(entity =>
        {
            entity.HasOne(ms => ms.Match)
                .WithMany()
                .HasForeignKey(ms => ms.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Throw
        modelBuilder.Entity<Throw>(entity =>
        {
            entity.HasOne(t => t.MatchSession)
                .WithMany(ms => ms.Throws)
                .HasForeignKey(t => t.MatchSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.Player)
                .WithMany()
                .HasForeignKey(t => t.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
