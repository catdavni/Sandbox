using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpDxSandbox.Window
{
    /// <summary>
    /// Use this class to implement overloads, which need to accept <see cref="string"/> or <see cref="FormattableString"/>.
    /// </summary>
    /// <remarks>
    /// Without this class:
    ///     void Foo(string s);
    ///     void Foo(FormattableString fs);
    ///
    ///     Foo("Hello!"); // calls first overload.
    ///     Foo($"Hello!"); // calls first overload.
    ///     Foo($"Hello! {x}"); // calls first overload.
    /// 
    /// With this class:
    ///     void Foo(StringBox sb);
    ///     void Foo(FormattableString fs);
    ///
    ///     Foo("Hello!"); // calls first overload.
    ///     Foo($"Hello!"); // calls second overload.
    ///     Foo($"Hello! {x}"); // calls second overload.
    ///
    /// Having zero overhead penalty!
    /// </remarks>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Is not intended to be compared.")]
    public readonly struct StringBox
    {
        // It is plain filed instead of property to prevent
        // any non-inlined property getter issues and make it
        // as fast as possible. As a result, it is compiled out
        // and has zero overhead as compared to plain string.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields", Justification = "Read the comment above.")]
        public readonly string Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringBox(string value)
        {
            Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "It is needed to be visible during overload resolution.")]
        public static implicit operator StringBox(string s) => new(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "It is needed to be visible during overload resolution.")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "fs", Justification = "Is not planned to be used -- should throw.")]
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "Not expected to be called by any means,")]
        public static implicit operator StringBox(FormattableString fs) => throw new InvalidOperationException();
    }
}