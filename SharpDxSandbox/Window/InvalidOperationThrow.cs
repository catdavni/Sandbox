using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SharpDxSandbox.Window;

[SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "It is not intended for comparison.")]
public readonly struct InvalidOperationThrow
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OrThrow(FormattableString message) => OrThrow(message.ToString(CultureInfo.InvariantCulture));

    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "It is not static to enforce callee to have an instance.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OrThrow(StringBox message) => throw new InvalidOperationException(message.Value);
}