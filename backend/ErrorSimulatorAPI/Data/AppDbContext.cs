using ErrorSimulatorAPI.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.AccountNumber).IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionId).IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Reference).IsUnique();

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.FromUser)
            .WithMany()
            .HasForeignKey(t => t.FromUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.ToUser)
            .WithMany()
            .HasForeignKey(t => t.ToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        var user1Id = new Guid("a1b2c3d4-0000-0000-0000-000000000001");
        var user2Id = new Guid("a1b2c3d4-0000-0000-0000-000000000002");
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = user1Id,
                FullName = "Alice Johnson",
                Email = "alice@example.com",
                AccountNumber = "ACC-000001",
                Currency = "USD",
                Balance = 1000000,
                ReservedBalance = 0,
                IsActive = true,
                CreatedAt = seedDate
            },
            new User
            {
                Id = user2Id,
                FullName = "Bob Smith",
                Email = "bob@example.com",
                AccountNumber = "ACC-000002",
                Currency = "USD",
                Balance = 10000,
                ReservedBalance = 0,
                IsActive = true,
                CreatedAt = seedDate
            }
        );
    }
}
