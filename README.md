# Base16.ErrGen

A C# source generator that generates structured error types from `[Error]` attribute templates. No runtime dependency required — all types are source-generated.

## Supported Frameworks

- .NET Framework 4.7.2+
- .NET Framework 4.8.1+
- .NET 8.0
- .NET 9.0
- .NET 10.0

## Installation

```
dotnet add package Base16.ErrGen
```

## Usage

Define a `partial record struct` or `partial record` decorated with one or more `[Error]` attributes. Each attribute takes a message template string:

```csharp
[Error("User '{Name:String}' was not found")]
[Error("User with ID {Id:Int32} was not found")]
public partial record struct UserNotFoundError;
```

Base16.ErrGen generates a `Message` property, typed properties for each template argument, and static factory methods:

```csharp
// Generated code
public partial record struct UserNotFoundError
{
    public String Message { get; private set; } = default!;
    public String Name { get; private set; } = default!;
    public Int32 Id { get; private set; } = default!;

    public UserNotFoundError() { }

    public UserNotFoundError(String message)
    {
        Message = message;
    }

    public static UserNotFoundError FromName(String name)
    {
        return new UserNotFoundError
        {
            Message = String.Concat("User '", name, "' was not found"),
            Name = name,
        };
    }

    public static UserNotFoundError FromId(Int32 id)
    {
        return new UserNotFoundError
        {
            Message = String.Concat("User with ID ", id, " was not found"),
            Id = id,
        };
    }
}
```

You can then create errors using the generated factory methods:

```csharp
var error = UserNotFoundError.FromName("alice");
Console.WriteLine(error.Message); // User 'alice' was not found
Console.WriteLine(error.Name);    // alice

var error2 = UserNotFoundError.FromId(42);
Console.WriteLine(error2.Message); // User with ID 42 was not found
Console.WriteLine(error2.Id);      // 42
```

## Record Structs vs Record Classes

The `[Error]` attribute works on both `partial record struct` and `partial record` (class) types:

```csharp
// Record struct — value type, public parameterless constructor
[Error("Validation failed for '{Field:String}'")]
public partial record struct ValidationError;

// Record class — reference type, private parameterless constructor
[Error("Payment of {Amount:Decimal} failed")]
public partial record PaymentError;
```

The generated code differs slightly: record structs get a `public` parameterless constructor, while record classes get a `private` one.

## Access Modifiers

Both `public` and `internal` error types are supported:

```csharp
[Error("An unexpected error occurred: {Details:String}")]
internal partial record struct InternalError;
```

## Template Syntax

Templates use `{Name}` or `{Name:Type}` placeholders:

| Placeholder | Factory parameter | Description |
|---|---|---|
| `{Name}` | `Object? name` | Untyped argument (defaults to `Object?`) |
| `{Name:String}` | `String name` | Typed argument |

Multiple arguments in a single template produce a factory method named `From{Arg1}{Arg2}And{ArgN}`:

```csharp
[Error("{Method:String} {Path:String} returned {StatusCode:Int32}")]
public partial record struct HttpError;

// Generates:
// public static HttpError FromMethodPathAndStatusCode(
//     String method, String path, Int32 statusCode)
```

## Multiple Error Templates

Applying multiple `[Error]` attributes to a single type generates multiple factory methods:

```csharp
[Error("Connection to '{Host:String}' timed out after {TimeoutMs:Int32}ms")]
[Error("Connection to '{Host:String}' was refused")]
public partial record struct ConnectionError;

// Generates:
// public static ConnectionError FromHostAndTimeoutMs(String host, Int32 timeoutMs)
// public static ConnectionError FromHost(String host)
```

Note that with two arguments, the naming is `From{Arg1}And{Arg2}` — the "And" separator only appears before the last argument.

## Usage with OneOf

Generated error types pair naturally with [OneOf](https://github.com/mcintyre321/OneOf) for discriminated-union-style return types:

```csharp
using OneOf;

[Error("User '{Name:String}' was not found")]
public partial record struct UserNotFoundError;

[Error("Field '{FieldName:String}' must be between {Min:Int32} and {Max:Int32}")]
public partial record struct ValidationError;

public static OneOf<User, ValidationError, UserNotFoundError> CreateUser(String name, String email)
{
    if (String.IsNullOrWhiteSpace(name))
        return ValidationError.FromFieldNameMinAndMax("Name", 1, 100);

    return new User(name, email);
}
```

Handle results exhaustively with `Match`:

```csharp
var result = CreateUser("", "bob@example.com");
var message = result.Match(
    user => $"Created user: {user.Name}",
    validationErr => $"Validation failed: {validationErr.Message}",
    notFoundErr => $"Not found: {notFoundErr.Message}"
);
```

Or check for specific error types with `TryPickT` and `IsT0`/`AsT1`:

```csharp
if (result.TryPickT1(out var validationErr, out _))
{
    Console.WriteLine(validationErr.Message);
}
```
