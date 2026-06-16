using HackatonFiap.Users.Domain.Enums;

namespace HackatonFiap.Users.Domain.ValueObjects;

public sealed class Document
{
    public string Value { get; private set; }

    private Document() => Value = string.Empty; // EF

    private Document(string value) => Value = value;

    public static Document Create(string raw, PersonType type) => new(Normalize(raw));

    public static bool IsValid(string raw, PersonType type)
    {
        var digits = Normalize(raw);
        return type == PersonType.Individual ? IsValidCpf(digits) : IsValidCnpj(digits);
    }

    private static string Normalize(string raw) =>
        new string((raw ?? string.Empty).Where(char.IsDigit).ToArray());

    private static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
            return false;

        var digits = cpf.Select(c => c - '0').ToArray();
        var d1 = CheckDigit(digits, 9, 10);
        var d2 = CheckDigit(digits, 10, 11);
        return digits[9] == d1 && digits[10] == d2;
    }

    private static int CheckDigit(int[] digits, int count, int startWeight)
    {
        var sum = 0;
        for (var i = 0; i < count; i++)
            sum += digits[i] * (startWeight - i);

        var rem = sum % 11;
        return rem < 2 ? 0 : 11 - rem;
    }

    private static bool IsValidCnpj(string cnpj)
    {
        if (cnpj.Length != 14 || cnpj.Distinct().Count() == 1)
            return false;

        var digits = cnpj.Select(c => c - '0').ToArray();
        int[] w1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] w2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var d1 = CnpjDigit(digits, w1);
        var d2 = CnpjDigit(digits, w2);
        return digits[12] == d1 && digits[13] == d2;
    }

    private static int CnpjDigit(int[] digits, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
            sum += digits[i] * weights[i];

        var rem = sum % 11;
        return rem < 2 ? 0 : 11 - rem;
    }
}
