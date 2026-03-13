namespace Base16.ErrGen.Example;

// Single untyped argument
[Error("User '{Name}' was not found")]
public partial record struct UserNotFoundError;

// Single typed argument
[Error("Request failed with status code {StatusCode:Int32}")]
public partial record struct HttpError;

// Multiple typed arguments
[Error("Field '{FieldName:String}' must be between {Min:Int32} and {Max:Int32}")]
public partial record struct ValidationError;

// Multiple [Error] attributes on one type → multiple factory methods
[Error("Connection to '{Host:String}' timed out after {TimeoutMs:Int32}ms")]
[Error("Connection to '{Host:String}' was refused")]
public partial record struct ConnectionError;

// Multiple [Error] attributes with different argument names to avoid factory name conflicts
[Error("File '{Path:String}' does not exist")]
[Error("Access denied to file at '{Location:String}'")]
public partial record struct FileError;

// Internal visibility
[Error("An unexpected error occurred: {Details:String}")]
internal partial record struct InternalError;

// --- Record classes ---

// Record class with single typed argument
[Error("Payment of {Amount:Decimal} failed")]
public partial record PaymentError;

// Record class with multiple [Error] attributes
[Error("Session '{SessionId:String}' has expired")]
[Error("Session was revoked by '{Admin:String}'")]
public partial record SessionError;
