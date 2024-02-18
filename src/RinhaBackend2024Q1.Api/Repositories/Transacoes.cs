using Dapper;
using Npgsql;
using RinhaBackend2024Q1.Api.Models.Requests;
using RinhaBackend2024Q1.Api.Models.Responses;
using RinhaBackend2024Q1.Api.Models.Tables;
using System.Data;

namespace RinhaBackend2024Q1.Api.Repositories;

public class Transacoes : IRegistradorDeTransacoes
{
    private const int MaxRetries = 20;

    private readonly IDbConnection _connection;

    public Transacoes(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    public async Task<(bool Sucesso, SaldoAtual SaldoAtual)> TenteAdicionarAsync(
        int clienteId,
        TransacaoRequest transacao)
    {
        var retries = 0;
        while (true)
        {
            using var transaction = _connection.BeginTransaction(IsolationLevel.ReadCommitted);
            {
                var cliente = await RetornaSaldoAtualAsync(clienteId);
                var novoSaldo = 0;
                if (transacao.Tipo.Equals("c", StringComparison.OrdinalIgnoreCase))
                {
                    novoSaldo = cliente!.SaldoAtual + transacao.Valor;
                }
                else
                {
                    novoSaldo = cliente!.SaldoAtual - transacao.Valor;
                }

                if ((cliente.Limite * -1) > novoSaldo)
                {
                    transaction.Rollback();
                    return (false, new SaldoAtual
                    {
                        Limite = cliente.Limite,
                        Saldo = cliente.SaldoAtual,
                    });
                }

                cliente.SaldoAtual = novoSaldo;

                try
                {
                    var atualizado = await TenteAtualizarSaldoAsync(
                        transaction,
                        cliente);

                    await RegistrarTransacaoAsync(transaction, cliente, transacao);
                    if (atualizado)
                    {
                        transaction.Commit();
                        return (true, new()
                        {
                            Limite = cliente.Limite,
                            Saldo = cliente.SaldoAtual,
                        });
                    }

                    transaction.Rollback();
                }
                catch (PostgresException ex)
                    when (ex.ErrorCode == -2147467259)
                {
                    retries++;
                    await Task.Delay(100);
                    if (retries > MaxRetries)
                    {
                        throw new Exception();
                    }
                }
            }
        }
    }

    public async Task<Extrato?> GetExtratoAsync(int id)
    {
        var extrato = new Extrato();
        await _connection.QueryAsync<ViewExtrato, TransacaoResponse, Extrato?>(
            sql: Statements.Extrato,
            map: (viewExtrato, transacao) =>
            {
                extrato.Saldo ??= new Saldo
                {
                    Limite = viewExtrato.Limite,
                    DataExtrato = viewExtrato.DataExtrato,
                    Total = viewExtrato.Total,
                };

                extrato.UltimasTransacoes.Add(transacao);
                return default;
            },
            param: new { id },
            splitOn: "transacaoid");

        return extrato;
    }

    private async Task<TabelaCliente?> RetornaSaldoAtualAsync(int id) =>
        await _connection.QueryFirstOrDefaultAsync<Models.Tables.TabelaCliente>(
            Statements.PesquisaClientePorId,
            new
            {
                id,
            });

    private async Task<bool> TenteAtualizarSaldoAsync(
        IDbTransaction trans,
        TabelaCliente cliente)
    {
        var registrosAtualizados = await _connection.ExecuteAsync(
            sql: Statements.AtualizarSaldo,
            param: new
            {
                cliente.Id,
                cliente.SaldoAtual,
                cliente.Versao,
                NovaVersao = cliente.Versao + 1,
            },
            transaction: trans);

        return registrosAtualizados == 1;
    }

    private async Task RegistrarTransacaoAsync(
        IDbTransaction trans,
        TabelaCliente cliente,
        TransacaoRequest transacao)
    {
        await _connection.ExecuteAsync(
            sql: Statements.AdicionarTransacao,
            param: new
            {
                cliente.Id,
                transacao.Valor,
                transacao.Tipo,
                transacao.Descricao,
                Versao = cliente.Versao + 1,
            },
            transaction: trans);
    }

    public async Task TesteBanco()
    {
        await _connection.ExecuteAsync("Select 1");
    }
}
