using System.ComponentModel;

namespace SharpDxSandbox.Interop
{
    internal static class Extensions
    {
        internal static IntPtr ThrowWinErrorIfNullPtr(this IntPtr value)
        {
            if (value != IntPtr.Zero)
            {
                return value;
            }

            throw new Win32Exception();
        }

        internal static void ThrowWinErrorIfFalse(this bool isTrue)
        {
            if (!isTrue)
            {
                throw new Win32Exception();
            }
        }

        internal static ushort ThrowWinErrorIfZero(this ushort value)
        {
            if (value != 0)
            {
                return value;
            }

            throw new Win32Exception();
        }
    }
}