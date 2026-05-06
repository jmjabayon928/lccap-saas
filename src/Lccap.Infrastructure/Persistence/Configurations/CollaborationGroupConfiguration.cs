using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class CollaborationGroupConfiguration : IEntityTypeConfiguration<CollaborationGroup>
{
    public void Configure(EntityTypeBuilder<CollaborationGroup> builder)
    {
        builder.ToTable("collaboration_groups");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken();

        builder.Property(e => e.AccountId).HasColumnName("account_id").IsRequired();

        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("ix_collaboration_groups_account");

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .HasConstraintName("fk_collaboration_groups_account")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

