using System;
using Base16.ErrGen.Utils;

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
);
