# Base16.ErrGen

A C# source generator that generates structured error types from `[Error]` attribute templates. No runtime dependency required — all types are source-generated.

## Installation

```
dotnet add package Base16.ErrGen
```

## Usage

Define a `partial record struct` decorated with one or more `[Error]` attributes. Each attribute takes a message template string:

```csharp
using Base16.ErrGen;

[Error("User '{Name:String}' was not found")]
[Error("User with ID {Id:Int32} was not found")]
public partial record struct UserNotFoundError;
```

Base16.ErrGen generates a `Message` property and static factory methods:

```csharp
// Generated code
public partial record struct UserNotFoundError
{
    public String Message { get; private set; }

    public static UserNotFoundError FromName(String? name)
    {
        return new(String.Concat("User '", name, "' was not found"));
    }

    public static UserNotFoundError FromId(Int32? id)
    {
        return new(String.Concat("User with ID ", id, " was not found"));
    }
}
```

You can then create errors using the generated factory methods:

```csharp
var error = UserNotFoundError.FromName("alice");
Console.WriteLine(error.Message); // User 'alice' was not found

var error2 = UserNotFoundError.FromId(42);
Console.WriteLine(error2.Message); // User with ID 42 was not found
```

## Template syntax

Templates use `{Name}` or `{Name:Type}` placeholders:

| Placeholder | Factory parameter | Description |
|---|---|---|
| `{Name}` | `Object? name` | Untyped argument (defaults to `Object`) |
| `{Name:String}` | `String? name` | Typed argument |

Multiple arguments in a single template produce a factory method named `From{Arg1}And{Arg2}...`:

```csharp
[Error("{Method:String} {Path:String} returned {StatusCode:Int32}")]
public partial record struct HttpError;

// Generates:
// public static HttpError FromMethodAndPathAndStatusCode(
//     String? method, String? path, Int32? statusCode)
```
