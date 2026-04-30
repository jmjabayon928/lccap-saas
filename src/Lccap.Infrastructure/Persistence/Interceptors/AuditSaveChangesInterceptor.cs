using System.Collections.Concurrent;
using System.Reflection;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lccap.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps auditing columns matching common naming conventions (<c>*Utc</c>, <c>*ByUserId</c>) when declared on entity types.
/// Safe with empty <see cref="ICurrentUserContext"/> (stores null user ids until authentication is wired).
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    internal const string CreatedAtUtc = nameof(CreatedAtUtc);
    internal const string UpdatedAtUtc = nameof(UpdatedAtUtc);
    internal const string CreatedByUserId = nameof(CreatedByUserId);
    internal const string UpdatedByUserId = nameof(UpdatedByUserId);
    internal const string DeletedAtUtc = nameof(DeletedAtUtc);
    internal const string DeletedByUserId = nameof(DeletedByUserId);

    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> Cache = new();

    private readonly IClock _clock;
    private readonly ICurrentUserContext _currentUserContext;

    public AuditSaveChangesInterceptor(IClock clock, ICurrentUserContext currentUserContext)
    {
        _clock = clock;
        _currentUserContext = currentUserContext;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = _clock.UtcNow;
        var userId = _currentUserContext.UserId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Unchanged or
                EntityState.Detached)
            {
                continue;
            }

            var entity = entry.Entity;

            switch (entry.State)
            {
                case EntityState.Added:
                    TryAssign(entity, CreatedAtUtc, now);
                    TryAssign(entity, UpdatedAtUtc, now);
                    TryAssignGuid(entity, CreatedByUserId, userId);
                    TryAssignGuid(entity, UpdatedByUserId, userId);
                    break;
                case EntityState.Modified:
                    TryAssign(entity, UpdatedAtUtc, now);
                    TryAssignGuid(entity, UpdatedByUserId, userId);
                    break;
                case EntityState.Deleted:
                    TryAssign(entity, DeletedAtUtc, now);
                    TryAssignGuid(entity, DeletedByUserId, userId);
                    break;
            }
        }
    }

    private static void TryAssign(object entity, string propertyName, DateTimeOffset value)
    {
        var property = ResolveProperty(entity, propertyName);
        if (property is null || !property.CanWrite)
        {
            return;
        }

        if (property.PropertyType == typeof(DateTimeOffset))
        {
            property.SetValue(entity, value);
            return;
        }

        if (property.PropertyType == typeof(DateTimeOffset?))
        {
            property.SetValue(entity, (DateTimeOffset?)value);
        }
    }

    private static void TryAssignGuid(object entity, string propertyName, Guid? value)
    {
        var property = ResolveProperty(entity, propertyName);
        if (property is null || !property.CanWrite)
        {
            return;
        }

        if (property.PropertyType == typeof(Guid))
        {
            if (value is { } v)
            {
                property.SetValue(entity, v);
            }

            return;
        }

        if (property.PropertyType == typeof(Guid?))
        {
            property.SetValue(entity, value);
        }
    }

    private static PropertyInfo? ResolveProperty(object entity, string propertyName)
    {
        var runtimeType = entity.GetType();
        return Cache.GetOrAdd(
            (runtimeType, propertyName),
            key =>
            {
                for (var type = key.Type; type is not null; type = type.BaseType)
                {
                    var property = type.GetRuntimeProperty(key.Name);
                    if (property is not null)
                    {
                        return property;
                    }
                }

                return null;
            });
    }
}
