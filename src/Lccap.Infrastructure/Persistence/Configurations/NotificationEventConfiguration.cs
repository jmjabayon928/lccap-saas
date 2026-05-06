using System.Text.Json;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class NotificationEventConfiguration : IEntityTypeConfiguration<NotificationEvent>
{
    public void Configure(EntityTypeBuilder<NotificationEvent> builder)
    {
        builder.ToTable("notification_events", t =>
        {
            t.HasCheckConstraint("ck_event_type_not_empty", "length(TRIM(BOTH FROM event_type)) > 0");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken();

        builder.Property(e => e.AccountId).HasColumnName("account_id");

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        var payloadConverter = new ValueConverter<JsonDocument, string>(
            v => v.RootElement.GetRawText(),
            v => JsonDocument.Parse(v, new JsonDocumentOptions()));

        builder.Property(e => e.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(payloadConverter);

        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired();

        builder.HasIndex(e => e.PayloadJson)
            .HasDatabaseName("ix_notification_events_json")
            .HasMethod("GIN");

        builder.HasIndex(e => e.EventType)
            .HasDatabaseName("ix_notification_events_type");

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .HasConstraintName("fk_notification_events_account")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .HasConstraintName("fk_notification_events_created_by_user")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .HasConstraintName("fk_notification_events_updated_by_user")
            .OnDelete(DeleteBehavior.SetNull);
    }
}

