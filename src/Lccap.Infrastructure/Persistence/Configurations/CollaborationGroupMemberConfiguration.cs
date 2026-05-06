using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class CollaborationGroupMemberConfiguration : IEntityTypeConfiguration<CollaborationGroupMember>
{
    public void Configure(EntityTypeBuilder<CollaborationGroupMember> builder)
    {
        builder.ToTable("collaboration_group_members");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken();

        builder.Property(e => e.GroupId).HasColumnName("group_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        builder.Property(e => e.AccountId).HasColumnName("account_id");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.Property(e => e.Role).HasDefaultValue("Member");

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .HasConstraintName("fk_collaboration_group_members_account")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .HasConstraintName("fk_collaboration_group_members_group")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasConstraintName("fk_collaboration_group_members_user")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

