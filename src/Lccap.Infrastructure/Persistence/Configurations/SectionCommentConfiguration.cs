using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class SectionCommentConfiguration : IEntityTypeConfiguration<SectionComment>
{
    public void Configure(EntityTypeBuilder<SectionComment> builder)
    {
        _ = builder.ToTable(
            "section_comments",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_section_comments_text_not_blank", "length(trim(both from comment_text)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_section_comments_type",
                    "comment_type::text = ANY (ARRAY['General','DataGap','Validation','RevisionRequest']::character varying[]::text[])");
            });

        builder.ConfigureBaseEntity();

        _ = builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");

        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.PlanId).HasColumnName("plan_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.SectionKey).HasColumnName("section_key").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.CommentType).HasColumnName("comment_type").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.CommentText).HasColumnName("comment_text").HasColumnType("text").IsRequired();
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.ResolvedByUserId).HasColumnName("resolved_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsResolved).HasColumnName("is_resolved").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken()
            .HasDefaultValueSql("public.gen_random_bytes(8)");

        _ = builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_section_comments_plan");

        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_section_comments_account");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_section_comments_created_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.ResolvedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_section_comments_resolved_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_section_comments_updated_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_section_comments_deleted_by");
    }
}

