namespace Lccap.Application.Common.Interfaces;

/// <summary>
/// Abstraction for deterministic time in tests; production uses <see cref="SystemClock"/>.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
