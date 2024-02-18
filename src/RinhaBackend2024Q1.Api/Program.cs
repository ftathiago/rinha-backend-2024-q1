using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RinhaBackend2024Q1.Api.Models;
using RinhaBackend2024Q1.Api.Models.Requests;
using RinhaBackend2024Q1.Api.Models.Responses;
using RinhaBackend2024Q1.Api.Models.Tables;
using RinhaBackend2024Q1.Api.Repositories;
using System.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// builder.Services.AddScoped<IDbConnection>(provider =>
//     new NpgsqlConnection(provider
//         .GetRequiredService<IConfiguration>()
//         .GetSection("ConnectionStrings")
//         .GetValue<string>("Database")));

var connString = builder.Configuration
    .GetSection("ConnectionStrings")
    .GetValue<string>("Database");

// builder.Services.AddScoped<IRegistradorDeTransacoes, Transacoes>();

var app = builder.Build();

var clients = new Cliente[]
{
    new (id: 1, limit: 100000),
    new (id: 2, limit: 80000),
    new (id: 3, limit: 1000000),
    new (id: 4, limit: 10000000),
    new (id: 5, limit: 500000),
};

var balanceApi = app.MapGroup("/clientes");

balanceApi.MapPost(
    "/{id}/transacoes",
    (
        [FromRoute] int id,
        [FromBody] TransacaoRequest transacao) =>
    {
        if (id < 1 || id > 5)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrEmpty(transacao.Descricao))
        {
            return Results.BadRequest();
        }

        if (transacao.Descricao.Length > 10)
        {
            return Results.BadRequest();
        }

        if (!(transacao.Tipo.Equals("c") || transacao.Tipo.Equals("d")))
        {
            return Results.BadRequest();
        }

        using var conn = new NpgsqlConnection(connString);

        conn.Open();
        try
        {
            var retries = 0;
            while (retries < 10)
            {
                using var transaction = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                conn.ExecuteScalar("select 1");
                var cliente = conn.QueryFirstOrDefault<TabelaCliente>(
                    Statements.PesquisaClientePorId,
                    new
                    {
                        id,
                    });

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
                    return Results.UnprocessableEntity(new SaldoAtual
                    {
                        Limite = cliente.Limite,
                        Saldo = cliente.SaldoAtual,
                    });
                }

                cliente.SaldoAtual = novoSaldo;

                try
                {
                    var atualizado = conn.Execute(
                        sql: Statements.AtualizarSaldo,
                        param: new
                        {
                            cliente.Id,
                            cliente.SaldoAtual,
                            cliente.Versao,
                            NovaVersao = cliente.Versao + 1,
                        },
                        transaction: transaction) == 1;

                    if (!atualizado)
                    {
                        transaction.Rollback();
                        retries++;
                        continue;
                    }

                    atualizado = conn.Execute(
                        sql: Statements.AdicionarTransacao,
                        param: new
                        {
                            cliente.Id,
                            transacao.Valor,
                            transacao.Tipo,
                            transacao.Descricao,
                            Versao = cliente.Versao + 1,
                        },
                        transaction: transaction) == 1;

                    if (atualizado)
                    {
                        transaction.Commit();
                        return Results.Ok(new SaldoAtual()
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
                    transaction.Rollback();
                    retries++;
                }
            }

            Console.WriteLine("Retries excedidos!");
            return Results.UnprocessableEntity();
        }
        finally
        {
            conn.Close();
        }
    });

balanceApi.MapGet("/{id}/extrato", ([FromRoute] int id) =>
{
    if (id < 1 || id > 5)
    {
        return Results.NotFound();
    }

    using var connection = new NpgsqlConnection(connString);
    connection.Open();
    try
    {
        var extrato = new Extrato();
        connection.Query<ViewExtrato, TransacaoResponse, Extrato?>(
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

        return Results.Ok(extrato);
    }
    finally
    {
        connection.Close();
    }
});

app.Run();

[JsonSerializable(typeof(Transacao))]
[JsonSerializable(typeof(Extrato))]
[JsonSerializable(typeof(Saldo))]
[JsonSerializable(typeof(TransacaoResponse))]
[JsonSerializable(typeof(SaldoAtual))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
