using Lccap.Application.Common.Interfaces;

namespace Lccap.Application.Common;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
