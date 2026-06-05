using Microsoft.EntityFrameworkCore;
using WinWigApp.Server.Models;

namespace WinWigApp.Server.Data;

public class WinWigDbContext : DbContext
{
    public WinWigDbContext(DbContextOptions<WinWigDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<Deposit> Deposits { get; set; }
    public DbSet<Strategy> Strategies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Transaction configuration
        modelBuilder.Entity<Transaction>()
            .HasKey(t => t.Id);
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Portfolio configuration
        modelBuilder.Entity<Portfolio>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Portfolio>()
            .HasOne(p => p.User)
            .WithMany(u => u.Portfolios)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deposit configuration
        modelBuilder.Entity<Deposit>()
            .HasKey(d => d.Id);
        modelBuilder.Entity<Deposit>()
            .HasOne(d => d.User)
            .WithMany(u => u.Deposits)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Strategy configuration
        modelBuilder.Entity<Strategy>()
            .HasKey(s => s.Id);
        modelBuilder.Entity<Strategy>()
            .HasOne(s => s.User)
            .WithMany(u => u.Strategies)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
