
using RinhaBackend2024Q1.Api.Models.Requests;
using RinhaBackend2024Q1.Api.Repositories;
using System.Data;

namespace RinhaBackend2024Q1.Tests.Repositories.TransacoesTest;

public class TenteAdicionarTest
{
    private readonly IDbConnection _connection;

    public TenteAdicionarTest()
    {
        _connection = Substitute.For<IDbConnection>();
    }

    [Fact]
    public async Task Deve_FazerSaque_Quando_HaSaldoSuficienteAsync()
    {
        // Given
        var cliente = 1;
        var transacao = new TransacaoRequest
        {
            Valor = -1000,
        };
        var transacoes = new Transacoes(_connection);

        // When
        var resultado = await transacoes.TenteAdicionarAsync(
            cliente,
            transacao);

        // Then
        resultado.Sucesso.Should().BeTrue();
        resultado.SaldoAtual.Saldo.Should().Be(transacao.Valor);
    }
}
