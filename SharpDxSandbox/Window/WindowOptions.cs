using System.Diagnostics.CodeAnalysis;

namespace SharpDxSandbox.Window;

[SuppressMessage("Microsoft.Naming", "CA1714:FlagsEnumsShouldHavePluralNames", Justification = "This naming is desired.")]
[Flags]
public enum WindowOptions
{
    None,
    TopMost = 1,
    HideCursor = 2
}