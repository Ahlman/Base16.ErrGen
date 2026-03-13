using Microsoft.CodeAnalysis;

namespace Base16.ErrGen.ErrorTypes;

internal static class ErrorDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidBaseType = new(
        id: "ERR001",
        title: "Invalid ErrorBaseType",
        messageFormat: "ErrorBaseType: '{0}' must be a class or interface",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor StructCannotInheritClass = new(
        id: "ERR002",
        title: "Record struct cannot inherit from class",
        messageFormat: "ErrorBaseType: record struct '{0}' cannot inherit from class '{1}'",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor AbstractBaseType = new(
        id: "ERR003",
        title: "Abstract base type not allowed",
        messageFormat: "ErrorBaseType: '{0}' must not be abstract",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor GenericBaseType = new(
        id: "ERR004",
        title: "Generic base type not allowed",
        messageFormat: "ErrorBaseType: '{0}' must not be a generic type",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MessagePropertyNotString = new(
        id: "ERR005",
        title: "Message property must be String",
        messageFormat: "ErrorBaseType: '{0}.Message' must be of type String",
        category: "Base16.ErrGen",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
