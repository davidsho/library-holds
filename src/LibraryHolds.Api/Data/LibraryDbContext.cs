using LibraryHolds.Api.Copies;
using LibraryHolds.Api.Holds;
using LibraryHolds.Api.Notifications;
using Microsoft.EntityFrameworkCore;

namespace LibraryHolds.Api.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Hold> Holds => Set<Hold>();
    public DbSet<Copy> Copies => Set<Copy>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hold>(hold =>
        {
            hold.ToTable("holds");
            hold.Property(h => h.Format).HasConversion<string>();
            hold.Property(h => h.Status).HasConversion<string>();
            hold.HasIndex(h => new { h.Title, h.Format, h.Status });
        });

        modelBuilder.Entity<Copy>(copy =>
        {
            copy.ToTable("copies");
            copy.Property(c => c.Id).ValueGeneratedNever();
            copy.Property(c => c.Format).HasConversion<string>();
            copy.Property(c => c.Location).HasConversion<string>();
        });

        modelBuilder.Entity<OutboxMessage>(message => message.ToTable("outbox_messages"));
    }
}
