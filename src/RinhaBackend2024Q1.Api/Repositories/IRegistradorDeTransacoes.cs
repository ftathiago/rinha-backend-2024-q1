using RinhaBackend2024Q1.Api.Models.Requests;
using RinhaBackend2024Q1.Api.Models.Responses;

namespace RinhaBackend2024Q1.Api.Repositories;

public interface IRegistradorDeTransacoes : IDisposable
{
    Task<(bool Sucesso, SaldoAtual SaldoAtual)> TenteAdicionarAsync(
        int clienteId,
        TransacaoRequest transacao);

    Task<Extrato?> GetExtratoAsync(int id);

    Task TesteBanco();
}
