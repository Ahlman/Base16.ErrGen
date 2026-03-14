using OneOf;

namespace Base16.ErrGen.Example;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Base16.ErrGen Example ===");
        Console.WriteLine();

        // Single untyped argument (Object?)
        var userErr = UserNotFoundError.FromName("alice");
        Console.WriteLine($"UserNotFoundError:  {userErr.Message}");

        // Single typed argument (Int32)
        var httpErr = HttpError.FromStatusCode(404);
        Console.WriteLine($"HttpError:          {httpErr.Message}");

        // Multiple typed arguments
        var validErr = ValidationError.FromFieldNameMinAndMax("Age", 0, 150);
        Console.WriteLine($"ValidationError:    {validErr.Message}");

        // Multiple factory methods from multiple [Error] attributes
        var timeoutErr = ConnectionError.FromHostAndTimeoutMs("db.example.com", 5000);
        var refusedErr = ConnectionError.FromHost("db.example.com");
        Console.WriteLine($"ConnectionError 1:  {timeoutErr.Message}");
        Console.WriteLine($"ConnectionError 2:  {refusedErr.Message}");

        // Multiple factory methods with different argument names
        var fileErr1 = FileError.FromPath("/etc/config.json");
        var fileErr2 = FileError.FromLocation("/var/secret.key");
        Console.WriteLine($"FileError 1:        {fileErr1.Message}");
        Console.WriteLine($"FileError 2:        {fileErr2.Message}");

        // Internal error type
        var internalErr = InternalError.FromDetails("null reference in pipeline");
        Console.WriteLine($"InternalError:      {internalErr.Message}");

        Console.WriteLine();

        // Accessing individual argument properties
        Console.WriteLine("--- Argument Properties ---");
        Console.WriteLine($"validErr.FieldName: {validErr.FieldName}");
        Console.WriteLine($"validErr.Min:       {validErr.Min}");
        Console.WriteLine($"validErr.Max:       {validErr.Max}");
        Console.WriteLine($"httpErr.StatusCode: {httpErr.StatusCode}");

        Console.WriteLine();

        // --- Record classes ---
        Console.WriteLine("--- Record Classes ---");

        // Record class with typed argument
        var payErr = PaymentError.FromAmount(49.99m);
        Console.WriteLine($"PaymentError:       {payErr.Message}");

        // Record class with multiple factory methods
        var sessionExp = SessionError.FromSessionId("abc-123");
        var sessionRev = SessionError.FromAdmin("admin@corp.com");
        Console.WriteLine($"SessionError 1:     {sessionExp.Message}");
        Console.WriteLine($"SessionError 2:     {sessionRev.Message}");

        Console.WriteLine();

        // --- Explicit base type inheritance ---
        Console.WriteLine("--- Explicit Base Type Inheritance ---");

        // Inherits from Error (has Message positional param)
        var dbErr = DatabaseError.FromHost("db.example.com");
        Console.WriteLine($"DatabaseError:      {dbErr.Message}");
        Console.WriteLine($"  is Error:      {dbErr is Error}");

        // Inherits from TracedError (has Message + TraceId positional params)
        var traceId = System.Guid.NewGuid();
        var authErr = AuthError.FromUserId(traceId, "user-42");
        Console.WriteLine($"AuthError:          {authErr.Message}");
        Console.WriteLine($"  TraceId:          {authErr.TraceId}");
        Console.WriteLine($"  is TracedError:   {authErr is TracedError}");

        Console.WriteLine();

        // --- OneOf<> integration ---
        Console.WriteLine("--- OneOf<> Integration ---");

        // Return type using OneOf to represent success or typed errors
        var createResult = CreateUser("", "bob@example.com");
        var message = createResult.Match(
            user => $"Created user: {user.Name}",
            validationErr => $"Validation failed: {validationErr.Message}",
            httpErr2 => $"HTTP error: {httpErr2.Message}"
        );
        Console.WriteLine($"CreateUser(\"\"):    {message}");

        var createResult2 = CreateUser("bob", "bob@example.com");
        var message2 = createResult2.Match(
            user => $"Created user: {user.Name}",
            validationErr => $"Validation failed: {validationErr.Message}",
            httpErr2 => $"HTTP error: {httpErr2.Message}"
        );
        Console.WriteLine($"CreateUser(\"bob\"): {message2}");

        // Using TryPickT to check for a specific error type
        var result = ProcessPayment(0m);
        if (result.TryPickT1(out var paymentErr, out _))
        {
            Console.WriteLine($"Payment error:      {paymentErr.Message}");
        }

        // Using IsT0/IsT1 to branch on success vs error
        var loginResult = Login("admin", "wrong");
        Console.WriteLine(
            $"Login result:       {(loginResult.IsT0 ? "Success" : loginResult.AsT1.Message)}"
        );
    }

    // OneOf as a return type: success value OR specific error types
    private static OneOf<User, ValidationError, HttpError> CreateUser(String name, String email)
    {
        if (String.IsNullOrWhiteSpace(name))
            return ValidationError.FromFieldNameMinAndMax("Name", 1, 100);

        // Simulate success
        return new User(name, email);
    }

    // OneOf with two cases: success OR a single error type
    private static OneOf<Decimal, PaymentError> ProcessPayment(Decimal amount)
    {
        if (amount <= 0)
            return PaymentError.FromAmount(amount);

        return amount;
    }

    // OneOf with session errors
    private static OneOf<User, SessionError> Login(String username, String password)
    {
        if (password != "secret")
            return SessionError.FromSessionId("failed-attempt");

        return new User(username, $"{username}@example.com");
    }
}

public record User(String Name, String Email);
