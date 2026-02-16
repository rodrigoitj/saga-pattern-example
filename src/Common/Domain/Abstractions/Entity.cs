namespace Shared.Domain.Abstractions;

/// <summary>
/// Base class for all entities in the application.
/// Implements core entity functionality and identity management.
/// </summary>
public abstract class Entity
{
    /// <inheritdoc/>
    protected Entity()
    {
    }
    
    /// <inheritdoc/>
    protected Entity(Guid id)
    {
        Id = id;
    }

    /// <inheritdoc/>
    public Guid Id { get; set; }
    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }
    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
        {
            return false;
        }

        return entity.Id == Id;
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc/>
    public static bool operator ==(Entity? a, Entity? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    /// <inheritdoc/>
    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
