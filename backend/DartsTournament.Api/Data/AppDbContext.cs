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
    public DbSet<Circuit> Circuits => Set<Circuit>();
    public DbSet<CircuitPointsRule> CircuitPointsRules => Set<CircuitPointsRule>();
    public DbSet<TournamentTeam> TournamentTeams => Set<TournamentTeam>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
        });

        // Player-User relationship (1:1 optional)
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithOne(u => u.LinkedPlayer)
                .HasForeignKey<Player>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(p => p.UserId).IsUnique();
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

        // Tournament
        modelBuilder.Entity<Tournament>(entity =>
        {
            // Défaut SQL explicite : sans lui, les lignes existantes avaient reçu 0
            // lors de la migration AddDoublesSupport
            entity.Property(t => t.TeamSize).HasDefaultValue(1);
        });

        // TournamentTeam (paires des tournois en double)
        modelBuilder.Entity<TournamentTeam>(entity =>
        {
            entity.HasOne(tt => tt.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(tt => tt.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(tt => tt.Player1)
                .WithMany()
                .HasForeignKey(tt => tt.Player1Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(tt => tt.Player2)
                .WithMany()
                .HasForeignKey(tt => tt.Player2Id)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(tt => tt.Group)
                .WithMany(g => g.Teams)
                .HasForeignKey(tt => tt.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Un joueur ne peut appartenir qu'à une paire par tournoi
            entity.HasIndex(tt => new { tt.TournamentId, tt.Player1Id }).IsUnique();
            entity.HasIndex(tt => new { tt.TournamentId, tt.Player2Id }).IsUnique();
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

            entity.HasOne(m => m.Team1)
                .WithMany()
                .HasForeignKey(m => m.Team1Id)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.Team2)
                .WithMany()
                .HasForeignKey(m => m.Team2Id)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(m => m.WinnerTeam)
                .WithMany()
                .HasForeignKey(m => m.WinnerTeamId)
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

        // Circuit
        modelBuilder.Entity<Circuit>(entity =>
        {
            // Supprimer un circuit détache les tournois, ne les supprime pas
            entity.HasMany(c => c.Tournaments)
                .WithOne(t => t.Circuit)
                .HasForeignKey(t => t.CircuitId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(c => c.PointsRules)
                .WithOne(r => r.Circuit)
                .HasForeignKey(r => r.CircuitId)
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
