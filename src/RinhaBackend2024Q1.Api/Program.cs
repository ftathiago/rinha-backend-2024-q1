using Dapper;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using RinhaBackend2024Q1.Api.Models;
using RinhaBackend2024Q1.Api.Models.Requests;
using RinhaBackend2024Q1.Api.Models.Responses;
using RinhaBackend2024Q1.Api.Models.Tables;
using RinhaBackend2024Q1.Api.Repositories;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var connString = builder.Configuration
    .GetSection("ConnectionStrings")
    .GetValue<string>("Database");

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

        using var conn = new NpgsqlConnection(connString);

        conn.Open();
        try
        {
            var retries = 0;
            while (retries < 10)
            {
                TransacaoEfetuada transacaoEfetuada = default;
                try
                {
                    transacaoEfetuada = conn.QueryFirst<TransacaoEfetuada>(
                        sql: @"
                            select operation_status as OperationStatus
                                , out_saldo_atual as SaldoAtual
                                , out_Limite as Limite
                            from efetuar_transacao(@Descricao, @Valor, @Tipo, @ClienteId)
                        ",
                        param: new
                        {
                            transacao.Descricao,
                            transacao.Valor,
                            transacao.Tipo,
                            ClienteId = id,
                        });
                }
                catch (System.Exception e)
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

public struct TransacaoEfetuada
{
    public int OperationStatus { get; set; }

    public int SaldoAtual { get; set; }

    public int Limite { get; set; }
}

[JsonSerializable(typeof(Transacao))]
[JsonSerializable(typeof(Extrato))]
[JsonSerializable(typeof(Saldo))]
[JsonSerializable(typeof(TransacaoResponse))]
[JsonSerializable(typeof(SaldoAtual))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
