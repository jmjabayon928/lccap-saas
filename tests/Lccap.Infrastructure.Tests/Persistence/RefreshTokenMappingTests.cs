using System;
using System.Linq;
using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class RefreshTokenMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55432;Database=lccap_refresh_token_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void RefreshToken_mapping_matches_schema()
    {
        using var ctx = CreateContext();

        var entity = ctx.Model.FindEntityType(typeof(RefreshToken))!;
        Assert.Equal("refresh_tokens", entity.GetTableName());
        Assert.Equal("public", entity.GetSchema());

        // Core columns present
        Assert.NotNull(entity.FindProperty(nameof(RefreshToken.TokenHash)));
        Assert.NotNull(entity.FindProperty(nameof(RefreshToken.TokenFamilyId)));
        Assert.NotNull(entity.FindProperty(nameof(RefreshToken.IssuedAtUtc)));
        Assert.NotNull(entity.FindProperty(nameof(RefreshToken.ExpiresAtUtc)));
        Assert.NotNull(entity.FindProperty(nameof(BaseEntity.RowVersion)));
    }

    [Fact]
    public void RefreshToken_has_required_token_hash()
    {
        using var ctx = CreateContext();

        var prop = ctx.Model.FindEntityType(typeof(RefreshToken))!.FindProperty(nameof(RefreshToken.TokenHash))!;
        Assert.False(prop.IsNullable);
        Assert.Equal(128, prop.GetMaxLength());
    }

    [Fact]
    public void RefreshToken_has_user_account_and_replaced_by_relationships()
    {
        using var ctx = CreateContext();

        var entity = ctx.Model.FindEntityType(typeof(RefreshToken))!;

        var userFk = entity.GetForeignKeys().Single(f => string.Equals(f.GetConstraintName(), "fk_refresh_tokens_user", StringComparison.Ordinal));
        Assert.Equal(DeleteBehavior.Cascade, userFk.DeleteBehavior);
        Assert.Same(typeof(User), userFk.PrincipalEntityType.ClrType);

        var accountFk = entity.GetForeignKeys().Single(f => string.Equals(f.GetConstraintName(), "fk_refresh_tokens_account", StringComparison.Ordinal));
        Assert.Equal(DeleteBehavior.Cascade, accountFk.DeleteBehavior);
        Assert.Same(typeof(Account), accountFk.PrincipalEntityType.ClrType);

        var replacedFk = entity.GetForeignKeys().Single(f => string.Equals(f.GetConstraintName(), "fk_refresh_tokens_replaced_by", StringComparison.Ordinal));
        Assert.Equal(DeleteBehavior.SetNull, replacedFk.DeleteBehavior);
        Assert.Same(typeof(RefreshToken), replacedFk.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void RefreshToken_has_expected_indexes_if_model_inspection_pattern_supports_it()
    {
        using var ctx = CreateContext();

        var indexes = ctx.Model.FindEntityType(typeof(RefreshToken))!.GetIndexes().ToList();

        // Unique partial on token_hash
        Assert.Contains(indexes, i =>
            string.Equals(i.GetDatabaseName(), "ux_refresh_tokens_token_hash_active", StringComparison.Ordinal)
            && i.IsUnique
            && string.Equals(i.GetFilter()?.Trim(), "is_deleted = false", StringComparison.OrdinalIgnoreCase));

        // User active filtered composite with DESC
        Assert.Contains(indexes, i =>
            string.Equals(i.GetDatabaseName(), "ix_refresh_tokens_user_active", StringComparison.Ordinal)
            && string.Equals(i.GetFilter()?.Trim(), "is_deleted = false AND revoked_at_utc IS NULL", StringComparison.OrdinalIgnoreCase));

        // Family index
        Assert.Contains(indexes, i =>
            string.Equals(i.GetDatabaseName(), "ix_refresh_tokens_family", StringComparison.Ordinal));

        // Account filtered index
        Assert.Contains(indexes, i =>
            string.Equals(i.GetDatabaseName(), "ix_refresh_tokens_account", StringComparison.Ordinal)
            && string.Equals(i.GetFilter()?.Trim(), "is_deleted = false", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RefreshToken_is_active_returns_true_only_for_non_deleted_non_revoked_non_expired_token()
    {
        var now = DateTimeOffset.UtcNow;
        var valid = new RefreshToken
        {
            TokenHash = "hash123",
            TokenFamilyId = Guid.NewGuid(),
            IssuedAtUtc = now.AddMinutes(-5),
            ExpiresAtUtc = now.AddHours(1),
            IsDeleted = false,
            RevokedAtUtc = null
        };

        Assert.True(valid.IsActive(now));
        Assert.True(valid.IsActive(now.AddMinutes(30)));

        var revoked = new RefreshToken { TokenHash = "h", TokenFamilyId = Guid.NewGuid(), IssuedAtUtc = now, ExpiresAtUtc = now.AddHours(1), RevokedAtUtc = now };
        Assert.False(revoked.IsActive(now));

        var deleted = new RefreshToken { TokenHash = "h", TokenFamilyId = Guid.NewGuid(), IssuedAtUtc = now, ExpiresAtUtc = now.AddHours(1), IsDeleted = true };
        Assert.False(deleted.IsActive(now));

        var expired = new RefreshToken { TokenHash = "h", TokenFamilyId = Guid.NewGuid(), IssuedAtUtc = now.AddHours(-2), ExpiresAtUtc = now.AddMinutes(-1) };
        Assert.False(expired.IsActive(now));
    }

    [Fact]
    public void RefreshToken_revoke_sets_revocation_fields()
    {
        var token = new RefreshToken
        {
            TokenHash = "hash456",
            TokenFamilyId = Guid.NewGuid(),
            IssuedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(2)
        };
        var beforeRow = token.RowVersion;

        var now = DateTimeOffset.UtcNow;
        var newTokenId = Guid.NewGuid();
        token.Revoke(now, "127.0.0.1", "rotated", newTokenId);

        Assert.Equal(now, token.RevokedAtUtc);
        Assert.Equal("127.0.0.1", token.RevokedByIp);
        Assert.Equal("rotated", token.RevokeReason);
        Assert.Equal(newTokenId, token.ReplacedByTokenId);
        Assert.Equal(now, token.UpdatedAtUtc);
        Assert.NotEqual(beforeRow, token.RowVersion); // rotated
    }
}
