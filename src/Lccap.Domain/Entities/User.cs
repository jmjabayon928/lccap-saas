using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class User : BaseEntity
{
    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    /// <remarks>varchar(255)</remarks>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    /// <remarks>varchar(200)</remarks>
    public string FullName { get; set; } = string.Empty;

    /// <remarks>Baseline enum-like varchar: SystemAdmin, NationalAdmin, AgencyAdmin, Admin, Planner, Reviewer, Viewer, PublicViewer.</remarks>
    public string Role { get; set; } = string.Empty;

    /// <remarks>'Active', 'Inactive', 'Suspended', or 'Invited'</remarks>
    public string Status { get; set; } = "Active";

    /// <remarks>Platform, Tenant, or Public.</remarks>
    public string UserScope { get; set; } = "Tenant";

    public DateTimeOffset? LastLoginAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    /// <remarks>Self-navigation for audit FKs referencing users rows.</remarks>
    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }

    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();

    public ICollection<Plan> CreatedPlans { get; } = new List<Plan>();

    /// <inheritdoc cref="Account.SoftDelete" />
    public void SoftDelete(DateTimeOffset atUtc, Guid? deletedByUserId)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = atUtc;
        DeletedByUserId = deletedByUserId;
    }
}
