using System;

namespace Base16.ErrGen.ErrorTypes;

internal sealed record ErrorBaseTypeInfo(
    String FullyQualifiedName,
    Boolean IsInterface,
    Boolean HasMessageProperty
);
