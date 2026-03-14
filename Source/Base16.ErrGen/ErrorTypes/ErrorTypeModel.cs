using System;
using System.Collections.Immutable;
using Base16.ErrGen.Utils;
using Microsoft.CodeAnalysis;

namespace Base16.ErrGen.ErrorTypes;

internal sealed record ErrorTypeModel(
    String Name,
    String FullName,
    String Namespace,
    String AccessModifier,
    Boolean IsStruct,
    Boolean IsPartial,
    EquatableArray<String> TemplateStrings,
    ErrorBaseTypeInfo? ExplicitBase
)
{
    /// <summary>
    ///     Location of the type declaration, used for diagnostic reporting.
    ///     Excluded from equality to preserve incremental pipeline caching.
    /// </summary>
    public Location? TypeLocation { get; init; }

    /// <summary>
    ///     Locations of each [Error] attribute, parallel to TemplateStrings.
    ///     Excluded from equality to preserve incremental pipeline caching.
    /// </summary>
    public ImmutableArray<Location?> TemplateLocations { get; init; }

    public Boolean Equals(ErrorTypeModel? other)
    {
        if (other is null)
            return false;

        return Name == other.Name
            && FullName == other.FullName
            && Namespace == other.Namespace
            && AccessModifier == other.AccessModifier
            && IsStruct == other.IsStruct
            && IsPartial == other.IsPartial
            && TemplateStrings.Equals(other.TemplateStrings)
            && ExplicitBase == other.ExplicitBase;
    }

    public override Int32 GetHashCode()
    {
        var hash = Name.GetHashCode();
        hash = (hash * 397) ^ FullName.GetHashCode();
        hash = (hash * 397) ^ Namespace.GetHashCode();
        hash = (hash * 397) ^ AccessModifier.GetHashCode();
        hash = (hash * 397) ^ IsStruct.GetHashCode();
        hash = (hash * 397) ^ IsPartial.GetHashCode();
        hash = (hash * 397) ^ TemplateStrings.GetHashCode();
        hash = (hash * 397) ^ (ExplicitBase?.GetHashCode() ?? 0);
        return hash;
    }
}
