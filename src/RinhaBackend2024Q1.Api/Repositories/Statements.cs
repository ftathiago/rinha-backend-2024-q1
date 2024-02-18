namespace RinhaBackend2024Q1.Api.Repositories;

public static class Statements
{
    public const string Extrato =
        @"
            select c.id 
                , c.saldo_atual as Total
                , c.limite 
                , t.id as TransacaoId
                , t.valor
                , t.tipo 
                , t.descricao 
                , t.realizada_em as RealizadaEm
            from clientes c
                left join transacoes t  on t.cliente_id = c.id 
            where c.id = @Id
            order by t.realizada_em desc
            limit 10        
        ";
}
