using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        _ = builder.ToTable(
            "refresh_tokens",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_refresh_tokens_hash_not_blank", "length(trim(both from token_hash)) > 0");
                _ = t.HasCheckConstraint("ck_refresh_tokens_expiry_after_issued", "expires_at_utc > issued_at_utc");
                _ = t.HasCheckConstraint("ck_refresh_tokens_revoke_reason", "revoke_reason IS NULL OR length(trim(both from revoke_reason)) > 0");
            });

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

        _ = builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid");

        _ = builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        _ = builder.Property(e => e.TokenFamilyId).HasColumnName("token_family_id").HasColumnType("uuid").IsRequired();

        _ = builder.Property(e => e.IssuedAtUtc).HasColumnName("issued_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        _ = builder.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamptz").IsRequired();

        _ = builder.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id").HasColumnType("uuid");

        _ = builder.Property(e => e.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(80);
        _ = builder.Property(e => e.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(80);
        _ = builder.Property(e => e.UserAgent).HasColumnName("user_agent");
        _ = builder.Property(e => e.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(100);

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValueSql("false");
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        // Relationships exactly as specified (no audit navs added to entity per spec)
        _ = builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_tokens_user");

        _ = builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_refresh_tokens_account");

        _ = builder.HasOne(e => e.ReplacedByToken)
            .WithMany()
            .HasForeignKey(e => e.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_refresh_tokens_replaced_by");

        // Indexes matching spec exactly (filtered/unique/partial + DESC)
        _ = builder.HasIndex(e => e.TokenHash)
            .HasDatabaseName("ux_refresh_tokens_token_hash_active")
            .IsUnique()
            .HasFilter("is_deleted = false");

        _ = builder.HasIndex(e => new { e.UserId, e.ExpiresAtUtc })
            .HasDatabaseName("ix_refresh_tokens_user_active")
            .IsDescending(false, true)
            .HasFilter("is_deleted = false AND revoked_at_utc IS NULL");

        _ = builder.HasIndex(e => new { e.TokenFamilyId, e.IssuedAtUtc })
            .HasDatabaseName("ix_refresh_tokens_family")
            .IsDescending(false, true);

        _ = builder.HasIndex(e => new { e.AccountId, e.IssuedAtUtc })
            .HasDatabaseName("ix_refresh_tokens_account")
            .IsDescending(false, true)
            .HasFilter("is_deleted = false");
    }
}
