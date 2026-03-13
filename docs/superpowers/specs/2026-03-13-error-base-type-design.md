# ErrorBaseType Assembly Attribute

Allow users to configure a base type (interface or class) that all generated error types inherit from.

## Usage

```csharp
[assembly: ErrorBaseType(typeof(IMyError))]
```

The generator resolves the `typeof()` argument at compile time to determine whether it's an interface or class. No explicit `Kind` parameter needed.

## Attribute Definition

Emitted during post-initialization alongside `ErrorAttribute`:

```csharp
[System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ErrorBaseTypeAttribute : System.Attribute
{
    public System.Type Type { get; }
    public ErrorBaseTypeAttribute(System.Type type) { Type = type; }
}
```

## Resolution Pipeline

In `Initialize()`, a `CompilationProvider` step:

1. Reads assembly attributes, finds `ErrorBaseTypeAttribute`
2. Extracts `INamedTypeSymbol` from the `typeof()` argument
3. Checks `TypeKind` — must be `Interface` or `Class`, otherwise emit `ERR001`
4. Checks if the type has a `Message` property
5. Returns a data object: `{ FullyQualifiedName, IsInterface, HasMessageProperty }` (or `null` if absent)

This is `Combine()`-d with the existing per-error-type pipeline.

## Code Generation

When base type info is present:

- **Type declaration**: emit `: IMyError` or `: MyBaseError` after the type name
- **Record structs + class base type**: emit `ERR002` (structs can't inherit from classes)
- **Message property**: skip generating if the base type declares a `Message` property
- All other generation (argument properties, factory methods, constructors) is unchanged

When absent, existing behavior is preserved.

## Diagnostics

| ID | Condition | Message |
|---|---|---|
| `ERR001` | `typeof()` argument can't be resolved or isn't a class/interface | `ErrorBaseType: '{0}' must be a class or interface` |
| `ERR002` | Base type is a class but the error type is a record struct | `ErrorBaseType: record struct '{0}' cannot inherit from class '{1}'` |

Both are compiler errors.

## Scope

- Applies to all `[Error]` types in the assembly. No per-type opt-out.
- Only `Message` is skipped when present on the base type. Argument properties are always generated.

## Test Plan

1. Interface base type — generated type includes `: IMyError`
2. Class base type on record class — includes `: MyBaseError`
3. Class base type on record struct — emits `ERR002`
4. Unresolvable type — emits `ERR001`
5. Interface with `Message` property — `Message` not generated on error type
6. Interface without `Message` — `Message` still generated
7. No attribute present — existing behavior unchanged (regression)
