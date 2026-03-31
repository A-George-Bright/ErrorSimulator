using ErrorSimulatorAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.TransactionId)
            .IsUnique();
    }
}