namespace Lccap.Application.Common.Interfaces;

/// <summary>
/// Request-scoped user identity snapshot. Implementations remain safe when unauthenticated (<see cref="IsAuthenticated"/> is false).
/// </summary>
public interface ICurrentUserContext
{
    Guid? UserId { get; }

    Guid? AccountId { get; }
    
    string? Role { get; }

    bool IsAuthenticated { get; }
}
