namespace FGC.Users.Domain.Abstractions;

public sealed class Error
{
    public string Code { get; }
    public string Description { get; }

    public Error(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public static readonly Error None = new("", "");

    public static implicit operator Result(Error error) => Result.Failure(error);
}
