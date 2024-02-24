using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RinhaBackend2024Q1.Api.Models;
using RinhaBackend2024Q1.Api.Models.Requests;
using RinhaBackend2024Q1.Api.Models.Responses;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Logging.ClearProviders();

var connString = builder.Configuration
    .GetSection("ConnectionStrings")
    .GetValue<string>("Database");

using var dataSource = new NpgsqlDataSourceBuilder(connString).Build();

var app = builder.Build();

var balanceApi = app.MapGroup("/clientes");

balanceApi.MapPost(
    "/{id}/transacoes",
    async (
        [FromRoute] int id,
        [FromBody] TransacaoRequest transacao) =>
    {
        if (id < 1 || id > 5)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrEmpty(transacao.Descricao))
        {
            return Results.UnprocessableEntity();
        }

        if (transacao.Descricao.Length > 10)
        {
            return Results.UnprocessableEntity();
        }

        if (!(transacao.Tipo.Equals("c") || transacao.Tipo.Equals("d")))
        {
            return Results.UnprocessableEntity();
        }

        await using var conn = await dataSource.OpenConnectionAsync();

        try
        {
            var retries = 0;
            while (retries < 10)
            {
                TransacaoEfetuada transacaoEfetuada = default;
                try
                {
                    var sqlTransacao =
                        @$"
                            select out_operation_status as OperationStatus
                                , out_saldo_atual as SaldoAtual
                                , out_Limite as Limite
                            from efetuar_transacao('{transacao.Descricao}', {transacao.Valor}, '{transacao.Tipo}', {id})
                        ";
                    using var command = new NpgsqlCommand(sqlTransacao, conn);
                    using var resultSet = await command.ExecuteReaderAsync();
                    await resultSet.ReadAsync();

                    transacaoEfetuada = new TransacaoEfetuada
                    {
                        OperationStatus = resultSet.GetInt32(0),
                        SaldoAtual = resultSet.GetInt32(1),
                        Limite = resultSet.GetInt32(2),
                    };
                }
                catch (Exception e)
                {
                    retries++;
                    Console.WriteLine(e.Message);
                    continue;
                }

                if (transacaoEfetuada.OperationStatus == 3)
                {
                    continue;
                }

                if (transacaoEfetuada.OperationStatus == 1)
                {
                    return Results.Ok(new SaldoAtual()
                    {
                        Limite = transacaoEfetuada.Limite,
                        Saldo = transacaoEfetuada.SaldoAtual,
                    });
                }

                if (transacaoEfetuada.OperationStatus == 2)
                {
                    return Results.UnprocessableEntity(new SaldoAtual()
                    {
                        Limite = transacaoEfetuada.Limite,
                        Saldo = transacaoEfetuada.SaldoAtual,
                    });
                }
            }

            Console.WriteLine("Retries excedidos");
            return Results.UnprocessableEntity();
        }
        finally
        {
            await conn.CloseAsync();
        }
    });

balanceApi.MapGet("/{id}/extrato", async ([FromRoute] int id) =>
{
    if (id < 1 || id > 5)
    {
        return Results.NotFound();
    }

    await using var connection = await dataSource.OpenConnectionAsync();

    try
    {
        var extrato = new Extrato();

        var sql =
            @$"
                select c.id
                     , c.saldo_atual as Total
                     , c.limite
                     , t.id as TransacaoId
                     , t.valor
                     , t.tipo
                     , t.descricao
                     , t.realizada_em as RealizadaEm
                from clientes c
                    left
                join transacoes t  on t.cliente_id = c.id
                where c.id = {id}
                order by t.realizada_em desc
                limit 10
            ";

        using var command = new NpgsqlCommand(sql, connection);
        using var resultSet = await command.ExecuteReaderAsync();

        while (await resultSet.ReadAsync())
        {
            extrato.Saldo ??= new Saldo
            {
                Limite = resultSet.GetInt32(2),
                DataExtrato = DateTime.Now,
                Total = resultSet.GetInt32(1),
            };

            if (!resultSet.IsDBNull(4))
            {
                extrato.UltimasTransacoes.Enqueue(new TransacaoResponse
                {
                    Descricao = resultSet.GetString(6),
                    RealizadaEm = resultSet.GetDateTime(7),
                    Tipo = resultSet.GetString(5),
                    Valor = resultSet.GetInt32(4),
                });
            }
        }

        return Results.Ok(extrato);
    }
    finally
    {
        await connection.CloseAsync();
    }
});

app.Run();

[JsonSerializable(typeof(Extrato))]
[JsonSerializable(typeof(Saldo))]
[JsonSerializable(typeof(SaldoAtual))]
[JsonSerializable(typeof(TransacaoResponse))]
[JsonSerializable(typeof(TransacaoRequest))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
