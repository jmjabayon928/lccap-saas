using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        _ = builder.ToTable(
            "audit_logs",
            "public",
            t => { _ = t.HasCheckConstraint("ck_audit_logs_action", "length(trim(both from action)) > 0"); });

        builder.ConfigureBaseEntity();

        _ = builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        _ = builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken()
            .HasDefaultValueSql("gen_random_bytes(8)");

        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid");

        _ = builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.EntityName).HasColumnName("entity_name").HasMaxLength(100).IsRequired();

        _ = builder.Property(e => e.EntityId).HasColumnName("entity_id").HasColumnType("uuid");

        _ = builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(50).IsRequired();

        _ = builder.Property(e => e.OldValuesJson).HasColumnName("old_values_json").HasColumnType("jsonb");

        _ = builder.Property(e => e.NewValuesJson).HasColumnName("new_values_json").HasColumnType("jsonb");

        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        _ = builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(80);

        _ = builder.Property(e => e.UserAgent).HasColumnName("user_agent");

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        _ = builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_audit_logs_account");

        _ = builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_audit_logs_user");

        _ = builder.HasIndex(e => new { e.AccountId, e.CreatedAtUtc })
            .HasDatabaseName("ix_audit_logs_account_created")
            .IsDescending(false, true);

        _ = builder.HasIndex(e => new { e.EntityName, e.EntityId }).HasDatabaseName("ix_audit_logs_entity");

        _ = builder.HasIndex(e => new { e.UserId, e.CreatedAtUtc })
            .HasDatabaseName("ix_audit_logs_user_created")
            .IsDescending(false, true);

        // NOTE: Batch 1 audit_log indexes are non-expression indexes and map directly in EF metadata.
    }
}
