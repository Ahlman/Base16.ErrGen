using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Base16.ErrGen.Utils;

internal readonly struct EquatableArray<T>(ImmutableArray<T> array)
    : IEquatable<EquatableArray<T>>,
        IEnumerable<T>
    where T : IEquatable<T>
{
    public ImmutableArray<T> Array { get; } = array;

    public Int32 Count => Array.Length;

    public T this[Int32 index] => Array[index];

    public Boolean Equals(EquatableArray<T> other)
    {
        if (Array.Length != other.Array.Length)
            return false;

        for (var i = 0; i < Array.Length; i++)
        {
            if (!Array[i].Equals(other.Array[i]))
                return false;
        }

        return true;
    }

    public override Boolean Equals(Object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override Int32 GetHashCode()
    {
        var hash = 0;
        foreach (var item in Array)
            hash = (hash * 397) ^ item.GetHashCode();
        return hash;
    }

    public ImmutableArray<T>.Enumerator GetEnumerator()
    {
        return Array.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)Array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Array).GetEnumerator();
    }
}
