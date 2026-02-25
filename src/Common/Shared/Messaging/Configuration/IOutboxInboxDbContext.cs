namespace Shared.Infrastructure.Messaging.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

/// <summary>
/// Interface that each service's DbContext must implement to support the outbox/inbox pattern.
/// Enables the outbox publisher, outbox processor, and inbox filter to work with any service's DbContext.
/// </summary>
public interface IOutboxInboxDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<InboxMessage> InboxMessages { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
