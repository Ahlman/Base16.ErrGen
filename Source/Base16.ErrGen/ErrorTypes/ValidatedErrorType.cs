using System;
using System.Collections.Generic;

namespace Base16.ErrGen.ErrorTypes;

/// <summary>
///     A fully validated and pre-processed error type, ready for code emission.
///     Produced by <see cref="ErrorTypesGenerator.ValidateAndParse"/>.
/// </summary>
internal sealed class ValidatedErrorType
{
    public String Name { get; }
    public String FullName { get; }
    public String Namespace { get; }
    public String AccessModifier { get; }
    public Boolean IsStruct { get; }
    public Boolean SkipMessage { get; }
    public String BaseTypeSuffix { get; }
    public String CtorAccess { get; }
    public String CtorParams { get; }
    public String BaseCtorCall { get; }
    public String CtorCallArgs { get; }
    public String? BaseFactoryParamDeclarations { get; }
    public IReadOnlyList<StringTemplate> Templates { get; }

    public ValidatedErrorType(
        String name,
        String fullName,
        String @namespace,
        String accessModifier,
        Boolean isStruct,
        Boolean skipMessage,
        String baseTypeSuffix,
        String ctorAccess,
        String ctorParams,
        String baseCtorCall,
        String ctorCallArgs,
        String? baseFactoryParamDeclarations,
        IReadOnlyList<StringTemplate> templates
    )
    {
        Name = name;
        FullName = fullName;
        Namespace = @namespace;
        AccessModifier = accessModifier;
        IsStruct = isStruct;
        SkipMessage = skipMessage;
        BaseTypeSuffix = baseTypeSuffix;
        CtorAccess = ctorAccess;
        CtorParams = ctorParams;
        BaseCtorCall = baseCtorCall;
        CtorCallArgs = ctorCallArgs;
        BaseFactoryParamDeclarations = baseFactoryParamDeclarations;
        Templates = templates;
    }
}
