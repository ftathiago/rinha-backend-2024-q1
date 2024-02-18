using RinhaBackend2024Q1.Api.Models;

namespace RinhaBackend2024Q1.Tests.Models;

public class ClientTest
{
    [Fact]
    public void ShouldAddMount()
    {
        const int InitialBalance = 100000;
        const int Add = 10;
        const int ExpectedAmount = InitialBalance + Add;

        var client = new Cliente(
            id: 1,
            limit: 100000,
            InitialBalance);

        client.AtualizarSaldo(Add);

        client.Saldo.Should().Be(ExpectedAmount);
    }

    [Fact]
    public void Should_NotUpdateAmount_When_HasNoFounds()
    {
        // Given
        const int InitialBalance = 0;
        const int WithdrawValue = -10;
        const int ExpectedBalance = InitialBalance;
        var client = new Cliente(
            id: 1,
            limit: 0,
            InitialBalance);

        // When
        client.AtualizarSaldo(WithdrawValue);

        // Then
        client.Saldo.Should().Be(ExpectedBalance);
    }
}
