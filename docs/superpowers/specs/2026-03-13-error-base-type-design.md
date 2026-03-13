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

1. Reads assembly attributes, finds `ErrorBaseTypeAttribute` (takes the first if multiple are found)
2. Extracts `INamedTypeSymbol` from the `typeof()` argument
3. Validates:
   - Must be `Interface` or `Class` (`TypeKind`), otherwise emit `ERR001`
   - Must not be abstract class, otherwise emit `ERR003`
   - Must not be a generic type, otherwise emit `ERR004`
4. Checks if the type has a `Message` property (using `GetMembers()` — direct members only, not inherited)
   - Must be of type `String` if present, otherwise emit `ERR005`
5. Returns an equatable record: `{ FullyQualifiedName, IsInterface, HasMessageProperty }` (or `null` if absent)

This is `Combine()`-d with the existing per-error-type pipeline.

## Code Generation

When base type info is present:

- **Type declaration**: emit `: FullyQualifiedName` after the type name (fully qualified to avoid namespace issues)
  - Interfaces on record structs: `public partial record struct Foo : My.Namespace.IMyError`
  - Interfaces on record classes: `public partial record Foo : My.Namespace.IMyError`
  - Classes on record classes: `public partial record Foo : My.Namespace.MyBaseError`
- **Record structs + class base type**: emit `ERR002` (structs can't inherit from classes)
- **Message property**: skip generating if the base type declares a `String Message` property
- **Constructors**: unchanged — base classes are restricted to non-abstract with a parameterless constructor (guaranteed by ERR003 + class constraint)
- All other generation (argument properties, factory methods) is unchanged

When absent, existing behavior is preserved.

## Diagnostics

| ID | Condition | Message |
|---|---|---|
| `ERR001` | `typeof()` argument can't be resolved or isn't a class/interface | `ErrorBaseType: '{0}' must be a class or interface` |
| `ERR002` | Base type is a class but the error type is a record struct | `ErrorBaseType: record struct '{0}' cannot inherit from class '{1}'` |
| `ERR003` | Base type is an abstract class | `ErrorBaseType: '{0}' must not be abstract` |
| `ERR004` | Base type is a generic type | `ErrorBaseType: '{0}' must not be a generic type` |
| `ERR005` | Base type has a `Message` property that is not of type `String` | `ErrorBaseType: '{0}.Message' must be of type String` |

All are compiler errors.

## Scope

- Applies to all `[Error]` types in the assembly. No per-type opt-out.
- Only `Message` is skipped when present on the base type. Argument properties are always generated.

## Test Plan

1. Interface base type — generated type includes `: IMyError`
2. Class base type on record class — includes `: MyBaseError`
3. Class base type on record struct — emits `ERR002`
4. Unresolvable type — emits `ERR001`
5. Interface with `String Message` property — `Message` not generated on error type
6. Interface without `Message` — `Message` still generated
7. Class with `String Message` property — `Message` not generated on error type
8. No attribute present — existing behavior unchanged (regression)
9. Abstract class base type — emits `ERR003`
10. Generic base type — emits `ERR004`
11. Base type with non-String `Message` property — emits `ERR005`
12. Base type in different namespace — fully qualified name appears in output
