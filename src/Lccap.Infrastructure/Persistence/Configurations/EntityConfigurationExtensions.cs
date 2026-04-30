using Lccap.Domain.Common.Entities;
using Lccap.Domain.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public static class EntityConfigurationExtensions
{
    public static EntityTypeBuilder<TEntity> ConfigureBaseEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : BaseEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnType("uuid");

        builder.Property(e => e.RowVersion)
            .HasColumnType("bytea")
            .IsConcurrencyToken();

        return builder;
    }

    /// <summary>Apply when mapping entities that implement tenant scoping metadata.</summary>
    public static EntityTypeBuilder<TEntity> ConfigureTenantEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ITenantEntity
    {
        builder.Property(e => e.TenantId).HasColumnType("uuid");
        _ = builder.HasIndex(e => e.TenantId);
        return builder;
    }

    /// <summary>
    /// Apply when mapping audit timestamps aligned with <see cref="Lccap.Domain.Common.Interfaces.IAuditableEntity"/> (PostgreSQL TIMESTAMPTZ).
    /// New entities aligned with EF interceptors typically map parallel <c>*Utc</c> columns stamped by <c>AuditSaveChangesInterceptor</c>.
    /// </summary>
    public static EntityTypeBuilder<TEntity> ConfigureAuditableColumns<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz");

        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz");

        builder.Property(e => e.CreatedByUserId).HasColumnType("uuid");

        builder.Property(e => e.UpdatedByUserId).HasColumnType("uuid");

        return builder;
    }

    /// <summary>Apply when entities support soft delete (PostgreSQL TIMESTAMPTZ nullable).</summary>
    public static EntityTypeBuilder<TEntity> ConfigureSoftDeleteColumn<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeleteEntity
    {
        builder.Property(e => e.DeletedAt).HasColumnType("timestamptz");
        return builder;
    }
}
