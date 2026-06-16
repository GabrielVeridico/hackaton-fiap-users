namespace FGC.Users.Domain.Abstractions;

public class Result<T> : Result
{
    public T Value { get; }

    private Result(T value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public new static Result<T> Failure(Error error) => new(default!, false, error);

    public static implicit operator Result<T>(T value) => Success(value);
}
