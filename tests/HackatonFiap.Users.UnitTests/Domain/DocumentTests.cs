using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace HackatonFiap.Users.UnitTests.Domain;

public class DocumentTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    public void IsValid_WithValidCpf_ReturnsTrue(string cpf)
    {
        Document.IsValid(cpf, PersonType.Individual).Should().BeTrue();
    }

    [Theory]
    [InlineData("111.111.111-11")] // todos iguais
    [InlineData("529.982.247-24")] // dígito errado
    [InlineData("123")]            // tamanho errado
    public void IsValid_WithInvalidCpf_ReturnsFalse(string cpf)
    {
        Document.IsValid(cpf, PersonType.Individual).Should().BeFalse();
    }

    [Theory]
    [InlineData("11.444.777/0001-61")]
    [InlineData("11444777000161")]
    public void IsValid_WithValidCnpj_ReturnsTrue(string cnpj)
    {
        Document.IsValid(cnpj, PersonType.Company).Should().BeTrue();
    }

    [Theory]
    [InlineData("11.111.111/1111-11")]
    [InlineData("11.444.777/0001-60")]
    public void IsValid_WithInvalidCnpj_ReturnsFalse(string cnpj)
    {
        Document.IsValid(cnpj, PersonType.Company).Should().BeFalse();
    }

    [Fact]
    public void Create_NormalizesToDigitsOnly()
    {
        Document.Create("529.982.247-25", PersonType.Individual).Value.Should().Be("52998224725");
    }
}
