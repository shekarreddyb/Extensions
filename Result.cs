public class Result
{
    protected readonly Exception? error;

    public bool Success { get; protected set; }

    public string Message { get => error?.Message ?? ""; }

    protected Result(bool success, Exception? error)
    {
        Success = success;
        this.error = error;
    }

    public Exception GetError() => error
        ?? throw new InvalidOperationException($"Error property for this Result not set.");

    public static Result Ok() => new Result(true, null);

    public static Result Error(Exception error)
    {
        return new Result(false, error);
    }

    public static implicit operator Result(Exception exception) =>
        new Result(false, exception);
}

public sealed class Result<TPayload> : Result
    where TPayload : class
{
    private readonly TPayload? payload;

    private Result(TPayload? payload, Exception? error, bool success) : base(success, error)
    {
        this.payload = payload;
    }

    public Result(TPayload payload) : base(true, null)
    {
        this.payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public Result(Exception error) : base(false, error)
    {
    }

    public TPayload GetOk() => Success
        ? payload ?? throw new InvalidOperationException($"Payload for Result<{typeof(TPayload)}> was not set.")
        : throw new InvalidOperationException($"Operation for Result<{typeof(TPayload)}> was not successful.");

    public new Exception GetError() => error
        ?? throw new InvalidOperationException($"Error property for Result<{typeof(TPayload)}> not set.");

    public /*new*/ static Result<TPayload> Ok(TPayload payload)
    {
        return new Result<TPayload>(payload, null, true);
    }

    public new static Result<TPayload> Error(Exception error)
    {
        return new Result<TPayload>(null, error, false);
    }

    public static implicit operator Result<TPayload>(TPayload payload) =>
        new(payload, null, true);

    public static implicit operator Result<TPayload>(Exception exception) =>
        new(exception);
}
