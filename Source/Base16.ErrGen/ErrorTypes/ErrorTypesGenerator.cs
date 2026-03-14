using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Base16.ErrGen.Extensions;
using Base16.ErrGen.Utils;
using Base16.ErrGen.Utils.Code;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Base16.ErrGen.ErrorTypes;

[Generator]
public sealed class ErrorTypesGenerator : IIncrementalGenerator
{
    private const String ErrorAttributeName = "Base16.ErrGen.ErrorAttribute";
    private const String ErrorBaseTypeAttributeName = "Base16.ErrGen.ErrorBaseTypeAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("Base16.ErrGen.IsExternalInit.g.cs", StaticTypes.IsExternalInit);
            ctx.AddSource("Base16.ErrGen.ErrorAttribute.g.cs", StaticTypes.ErrorAttribute);
            ctx.AddSource(
                "Base16.ErrGen.ErrorBaseTypeAttribute.g.cs",
                StaticTypes.ErrorBaseTypeAttribute
            );
        });

        var baseTypeInfo = context.CompilationProvider.Select(
            static (compilation, ct) =>
            {
                var assemblyAttributes = compilation.Assembly.GetAttributes();
                var attr = assemblyAttributes.FirstOrDefault(a =>
                    a.AttributeClass?.ToDisplayString() == ErrorBaseTypeAttributeName
                );

                if (attr is null)
                    return (Info: (ErrorBaseTypeInfo?)null, Diagnostic: (Diagnostic?)null);

                if (
                    attr.ConstructorArguments.Length == 0
                    || attr.ConstructorArguments[0].Value is not INamedTypeSymbol typeSymbol
                )
                {
                    return (
                        Info: null,
                        Diagnostic: Diagnostic.Create(
                            ErrorDiagnostics.InvalidBaseType,
                            attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                            attr.ConstructorArguments.Length > 0
                                ? attr.ConstructorArguments[0].Value?.ToString() ?? "unknown"
                                : "unknown"
                        )
                    );
                }

                if (
                    typeSymbol.TypeKind != TypeKind.Interface
                    && typeSymbol.TypeKind != TypeKind.Class
                )
                {
                    return (
                        Info: null,
                        Diagnostic: Diagnostic.Create(
                            ErrorDiagnostics.InvalidBaseType,
                            attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                            typeSymbol.ToDisplayString()
                        )
                    );
                }

                if (typeSymbol.IsGenericType)
                {
                    return (
                        Info: null,
                        Diagnostic: Diagnostic.Create(
                            ErrorDiagnostics.GenericBaseType,
                            attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                            typeSymbol.ToDisplayString()
                        )
                    );
                }

                if (typeSymbol.TypeKind == TypeKind.Class && !typeSymbol.IsRecord)
                {
                    return (
                        Info: null,
                        Diagnostic: Diagnostic.Create(
                            ErrorDiagnostics.BaseTypeMustBeRecord,
                            attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                            typeSymbol.ToDisplayString()
                        )
                    );
                }

                var messageProperty = FindPropertyInHierarchy(typeSymbol, "Message");

                if (
                    messageProperty is not null
                    && messageProperty.Type.SpecialType != SpecialType.System_String
                )
                {
                    return (
                        Info: null,
                        Diagnostic: Diagnostic.Create(
                            ErrorDiagnostics.MessagePropertyNotString,
                            attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation(),
                            typeSymbol.ToDisplayString()
                        )
                    );
                }

                return (Info: (ErrorBaseTypeInfo?)BuildBaseTypeInfo(typeSymbol), Diagnostic: null);
            }
        );

        var errorTypes = context.SyntaxProvider.ForAttributeWithMetadataName(
            ErrorAttributeName,
            predicate: (node, _) => node is RecordDeclarationSyntax,
            transform: static (ctx, _) =>
            {
                var symbol = (INamedTypeSymbol)ctx.TargetSymbol;

                ErrorBaseTypeInfo? explicitBase = null;
                if (
                    symbol.BaseType is
                    {
                        SpecialType: not SpecialType.System_Object
                            and not SpecialType.System_ValueType,
                    } baseType
                )
                {
                    explicitBase = BuildBaseTypeInfo(baseType);
                }

                var isPartial = symbol.DeclaringSyntaxReferences.Any(r =>
                    r.GetSyntax() is RecordDeclarationSyntax { Modifiers: var mods }
                    && mods.Any(m => m.Text == "partial")
                );

#pragma warning disable IDE0072 // Populate switch
                var accessModifier = symbol.DeclaredAccessibility switch
                {
                    Accessibility.Public => "public",
                    Accessibility.Internal => "internal",
                    _ => "public",
                };
#pragma warning restore IDE0072

                var errorAttributes = symbol
                    .GetAttributes()
                    .Where(x => x.AttributeClass!.ToDisplayString() == ErrorAttributeName)
                    .ToImmutableArray();

                var templateStrings = errorAttributes
                    .Select(x => x.ConstructorArguments[0].Value)
                    .OfType<String>()
                    .ToImmutableArray();

                var templateLocations = errorAttributes
                    .Select(x => x.ApplicationSyntaxReference?.GetSyntax().GetLocation())
                    .ToImmutableArray();

                return new ErrorTypeModel(
                    symbol.Name,
                    symbol.ToDisplayString(),
                    symbol.ContainingNamespace.ToDisplayString(),
                    accessModifier,
                    symbol.TypeKind == TypeKind.Struct,
                    isPartial,
                    new EquatableArray<String>(templateStrings),
                    explicitBase
                )
                {
                    TypeLocation = symbol.Locations.FirstOrDefault(),
                    TemplateLocations = templateLocations,
                };
            }
        );

        var combined = errorTypes.Combine(baseTypeInfo);

        context.RegisterSourceOutput(
            baseTypeInfo,
            static (context, result) =>
            {
                if (result.Diagnostic is not null)
                    context.ReportDiagnostic(result.Diagnostic);
            }
        );

        context.RegisterSourceOutput(
            combined,
            (context, pair) =>
            {
                var (model, assemblyBase) = pair;
                var hasExplicitBase = model.ExplicitBase is not null;
                var effectiveBase = model.ExplicitBase ?? assemblyBase.Info;
                GenerateErrorType(context, model, effectiveBase, hasExplicitBase);
            }
        );
    }

    private void GenerateErrorType(
        SourceProductionContext context,
        ErrorTypeModel model,
        ErrorBaseTypeInfo? baseTypeInfo,
        Boolean hasExplicitBase
    )
    {
        if (!model.IsPartial)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    ErrorDiagnostics.ErrorTypeMustBePartial,
                    model.TypeLocation,
                    model.Name
                )
            );
            return;
        }

        var cb = CSharpCodeBuilder.NewFile();

        var recordType = model.IsStruct ? "record struct" : "record";

        var baseTypeSuffix = "";
        if (baseTypeInfo is not null && !hasExplicitBase)
        {
            if (!baseTypeInfo.IsInterface && model.IsStruct)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ErrorDiagnostics.StructCannotInheritClass,
                        model.TypeLocation,
                        model.Name,
                        baseTypeInfo.FullyQualifiedName
                    )
                );
                return;
            }
            baseTypeSuffix = $" : {baseTypeInfo.FullyQualifiedName}";
        }

        cb.AppendLines(
            $"""
            using System;

            namespace {model.Namespace};

            {model.AccessModifier} partial {recordType} {model.Name}{baseTypeSuffix}
            """
        );

        var errorTemplates = new List<StringTemplate>();
        for (var i = 0; i < model.TemplateStrings.Count; i++)
        {
            var templateLocation =
                i < model.TemplateLocations.Length
                    ? model.TemplateLocations[i]
                    : model.TypeLocation;

            if (
                !StringTemplate.TryParse(
                    model.TemplateStrings[i],
                    out var parsed,
                    out var parseError
                )
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ErrorDiagnostics.InvalidTemplateSyntax,
                        templateLocation,
                        parseError
                    )
                );
                return;
            }

            errorTemplates.Add(parsed!);
        }

        var factoryNames = new HashSet<String>();
        for (var i = 0; i < errorTemplates.Count; i++)
        {
            var namePart = GetFactoryMethodName(errorTemplates[i]);

            if (!factoryNames.Add(namePart))
            {
                var templateLocation =
                    i < model.TemplateLocations.Length
                        ? model.TemplateLocations[i]
                        : model.TypeLocation;

                context.ReportDiagnostic(
                    Diagnostic.Create(
                        ErrorDiagnostics.DuplicateFactoryMethod,
                        templateLocation,
                        model.Name,
                        namePart
                    )
                );
                return;
            }
        }

        using (cb.PushScope())
        {
            var skipMessage = baseTypeInfo is { HasMessageProperty: true, IsInterface: false };
            if (!skipMessage)
                cb.AppendLine("public String Message { get; private set; } = default!;");
            cb.AppendLine();
            var templates = errorTemplates
                .SelectMany(x => x.Parts)
                .OfType<StringTemplateArgumentPart>()
                .DistinctBy(x => x.Name)
                .ToList();
            foreach (var template in templates)
            {
                cb.AppendLine(
                    $"public {template.Type ?? "Object?"} {template.Name} {{ get; private set; }} = default!;"
                );
                cb.AppendLine();
            }

            var ctorAccess = model.IsStruct ? "public" : "private";
            var ctorParams = baseTypeInfo?.BaseCtorParamDeclarations is { Length: > 0 } paramDecl
                ? paramDecl
                : "";
            var baseCtorCall = baseTypeInfo?.BaseCtorCallArgs is { Length: > 0 } baseCallArgs
                ? $" : base({baseCallArgs})"
                : "";
            cb.AppendLine($"{ctorAccess} {model.Name}({ctorParams}){baseCtorCall} {{ }}");
            cb.AppendLine();

            foreach (var template in errorTemplates)
            {
                var arguments = template
                    .Parts.OfType<StringTemplateArgumentPart>()
                    .DistinctBy(x => x.Name)
                    .ToList();

                var namePart = GetFactoryMethodName(template);

                var templateArgs = arguments.Select(x =>
                    $"{x.Type ?? "Object?"} {x.Name.ToCamelCase()}"
                );
                var allArgs = baseTypeInfo?.BaseFactoryParamDeclarations
                    is { Length: > 0 } factoryParams
                    ? String.Join(", ", new[] { factoryParams }.Concat(templateArgs))
                    : String.Join(", ", templateArgs);

                cb.AppendLine();

                var templateDisplay = String.Concat(
                    template.Parts.Select(p =>
                        p switch
                        {
                            StringTemplateLiteralPart lit => lit.Value,
                            StringTemplateArgumentPart arg => arg.Type is not null
                                ? $"{{{arg.Name}:{arg.Type}}}"
                                : $"{{{arg.Name}}}",
                            _ => "",
                        }
                    )
                );
                cb.AppendLine(
                    $"""
                    /// <summary>
                    ///     Creates a new <see cref="{model.Name}"/> with the message template:
                    ///     <para>
                    ///         <c>{EscapeXml(templateDisplay)}</c>
                    ///     </para>
                    /// </summary>
                    """
                );
                if (baseTypeInfo?.BaseFactoryParamDeclarations is { Length: > 0 } baseParamDecls)
                {
                    foreach (var decl in baseParamDecls.Split(','))
                    {
                        var parts = decl.Trim().Split(' ');
                        var paramName = parts[parts.Length - 1];
                        cb.AppendLine(
                            $"/// <param name=\"{paramName}\">The value passed to the base type constructor.</param>"
                        );
                    }
                }
                foreach (var arg in arguments)
                {
                    cb.AppendLine(
                        $"/// <param name=\"{arg.Name.ToCamelCase()}\">The value for the <c>{{{EscapeXml(arg.Name)}}}</c> placeholder.</param>"
                    );
                }
                cb.AppendLine(
                    $"/// <returns>A new instance of <see cref=\"{model.Name}\"/>.</returns>"
                );

                cb.AppendLine($"public static {model.Name} From{namePart}({allArgs})");
                using (cb.PushScope())
                {
                    cb.AppendLine($"var message = String.Concat(");
                    foreach (var templatePart in template.Parts)
                    {
                        var isLast = templatePart == template.Parts.Last();
                        var str = templatePart switch
                        {
                            StringTemplateLiteralPart part => $"\"{part.Value}\"",
                            StringTemplateArgumentPart part => $"{part.Name.ToCamelCase()}",
                            _ => throw new NotSupportedException(
                                $"Template part of type '{templatePart.GetType().Name}' is not supported."
                            ),
                        };

                        cb.AppendLineIndented($"{str}{(isLast ? "" : ",")}");
                    }
                    cb.AppendLine(");");
                    cb.AppendLine();

                    var ctorCallArgs = baseTypeInfo?.BaseCtorCallArgs is { Length: > 0 } callArgs
                        ? callArgs
                        : "";
                    cb.AppendLine($"return new {model.Name}({ctorCallArgs})");
                    using (cb.PushScopeExpression())
                    {
                        if (String.IsNullOrEmpty(ctorCallArgs))
                            cb.AppendLine($"Message = message,");
                        foreach (var argument in arguments)
                            cb.AppendLine($"{argument.Name} = {argument.Name.ToCamelCase()},");
                    }
                }
            }
        }

        context.AddSource($"{model.FullName}.g.cs", cb.ToString());
    }

    private static String GetFactoryMethodName(StringTemplate template)
    {
        var arguments = template
            .Parts.OfType<StringTemplateArgumentPart>()
            .DistinctBy(x => x.Name)
            .ToList();

        return arguments.Count switch
        {
            0 => String.Empty,
            1 => arguments[0].Name,
            _ => String.Concat([
                .. arguments.Select(x => x.Name).Take(arguments.Count - 1),
                "And",
                arguments.Last().Name,
            ]),
        };
    }

    private static IPropertySymbol? FindPropertyInHierarchy(
        INamedTypeSymbol typeSymbol,
        String name
    )
    {
        var current = typeSymbol;
        while (current is not null)
        {
            var prop = current
                .GetMembers()
                .OfType<IPropertySymbol>()
                .FirstOrDefault(p => p.Name == name);
            if (prop is not null)
                return prop;
            current = current.BaseType;
        }
        return null;
    }

    private static ErrorBaseTypeInfo BuildBaseTypeInfo(INamedTypeSymbol typeSymbol)
    {
        var messageProperty = FindPropertyInHierarchy(typeSymbol, "Message");

        var primaryCtor = typeSymbol
            .InstanceConstructors.Where(c =>
                !(
                    c.Parameters.Length == 1
                    && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, typeSymbol)
                )
            )
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();

        var ctorParams =
            primaryCtor?.Parameters
            ?? System.Collections.Immutable.ImmutableArray<IParameterSymbol>.Empty;

        var baseCtorParamDeclarations = String.Join(
            ", ",
            ctorParams.Select(p =>
                $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name.ToCamelCase()}"
            )
        );

        var baseCtorCallArgs = String.Join(", ", ctorParams.Select(p => p.Name.ToCamelCase()));

        var baseFactoryParamDeclarations = String.Join(
            ", ",
            ctorParams
                .Where(p =>
                    !(p.Name == "Message" && p.Type.SpecialType == SpecialType.System_String)
                )
                .Select(p =>
                    $"{p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {p.Name.ToCamelCase()}"
                )
        );

        return new ErrorBaseTypeInfo(
            typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeSymbol.TypeKind == TypeKind.Interface,
            messageProperty is not null,
            baseCtorParamDeclarations,
            baseCtorCallArgs,
            baseFactoryParamDeclarations
        );
    }

    private static String EscapeXml(String value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
