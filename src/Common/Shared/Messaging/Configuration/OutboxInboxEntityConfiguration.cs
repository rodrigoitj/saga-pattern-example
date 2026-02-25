namespace Shared.Infrastructure.Messaging.Configuration;

using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Configures the OutboxMessages and InboxMessages tables for Entity Framework.
/// Call <see cref="ConfigureOutboxInbox"/> from each service's DbContext.OnModelCreating.
/// </summary>
public static class OutboxInboxEntityConfiguration
{
    public static void ConfigureOutboxInbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();
            builder.Property(o => o.Type).IsRequired().HasMaxLength(500);
            builder.Property(o => o.Content).IsRequired();
            builder.Property(o => o.CreatedAtUtc).IsRequired();
            builder.Property(o => o.Error).HasMaxLength(2000);
            builder
                .HasIndex(o => new { o.ProcessedAtUtc, o.CreatedAtUtc })
                .HasDatabaseName("IX_OutboxMessages_ProcessedAtUtc_CreatedAtUtc");
            builder.ToTable("OutboxMessages");
        });

        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).ValueGeneratedNever();
            builder.Property(i => i.MessageId).IsRequired();
            builder.Property(i => i.ConsumerType).IsRequired().HasMaxLength(500);
            builder.Property(i => i.ProcessedAtUtc).IsRequired();
            builder
                .HasIndex(i => i.MessageId)
                .IsUnique()
                .HasDatabaseName("IX_InboxMessages_MessageId");
            builder.ToTable("InboxMessages");
        });
    }
}
