namespace SharpDxSandbox.Window;

public static class Verify
{
    /// <summary>
    /// Verifies the supplied condition boolean value and allows to throw <see cref="InvalidOperationException"/> if it is <c>false</c>.
    /// <code>
    /// Verify.IsTrue(requested &lt; size)?.OrThrow($&quot;{requested} &lt; {size}!&quot;);
    /// // or
    /// Verify.IsTrue(hasSpace)?.OrThrow(&quot;It is empty.&quot;);
    /// </code>
    /// </summary>
    /// <remarks>
    /// The trick is to use null-coalescing operator for returned nullable instance, to construct exception message
    /// only if condition is <c>false</c>.
    /// See <see cref="InvalidOperationThrow"/>.
    /// </remarks>
    /// <param name="condition">Condition to be verified.</param>
    /// <returns><c>null</c> if condition is <c>true</c>, or a valid throw instance otherwise.</returns>
    public static InvalidOperationThrow? IsTrue(bool condition) => condition ? null : new InvalidOperationThrow();

    /// <summary>
    /// Verifies the supplied condition boolean value and allows to throw <see cref="InvalidOperationException"/> if it is <c>true</c>.
    /// <code>
    /// Verify.IsFalse(hasSpace)?.OrThrow(&quot;It is not empty.&quot;);
    /// </code>
    /// </summary>
    /// <remarks>
    /// The trick is to use null-coalescing operator for returned nullable instance, to construct exception message
    /// only if condition is <c>false</c>.
    /// See <see cref="InvalidOperationThrow"/>.
    /// </remarks>
    /// <param name="condition">Condition to be verified.</param>
    /// <returns><c>null</c> if condition is <c>false</c>, or a valid throw instance otherwise.</returns>
    public static InvalidOperationThrow? IsFalse(bool condition) => !condition ? null : new InvalidOperationThrow();

    public static void IsPositive<T>(T value, string valueName)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) <= 0)
        {
            throw new InvalidOperationException(FormattableString.Invariant($"{valueName} ({value}) must be positive"));
        }
    }

    public static void IsPositiveOrZero<T>(T value, string valueName)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) < 0)
        {
            throw new InvalidOperationException(FormattableString.Invariant($"{valueName} ({value}) must be non-negative"));
        }
    }

    public static void IsNegative<T>(T value, string valueName)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) >= 0)
        {
            throw new InvalidOperationException(valueName + " must be negative");
        }
    }

    public static void IsNotNull<T>(T value, string valueName)
        where T : class
    {
        if (value == null)
        {
            throw new InvalidOperationException(valueName + " is null");
        }
    }

    public static void IsNotNull<T>(T? value, string valueName)
        where T : struct
    {
        if (value == null)
        {
            throw new InvalidOperationException(valueName + " is null");
        }
    }

    public static void IsNull<T>(T value, string valueName)
        where T : class
    {
        if (value != null)
        {
            throw new InvalidOperationException(valueName + " is not null");
        }
    }

    public static void IsNull<T>(T? value, string valueName)
        where T : struct
    {
        if (value != null)
        {
            throw new InvalidOperationException(valueName + " is not null");
        }
    }
}