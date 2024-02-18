namespace RinhaBackend2024Q1.Api.Repositories;

public static class Statements
{
    public const string PesquisaClientePorId =
        @"
            SELECT id
                 , nome
                 , limite
                 , saldo_atual as SaldoAtual
                 , versao 
            FROM clientes 
            where id = @id
        ";

    public const string AtualizarSaldo =
        @"
            UPDATE clientes SET 
                saldo_atual = @SaldoAtual, 
                versao = @NovaVersao
            WHERE id = @Id
              and versao = @Versao
        ";

    public const string AdicionarTransacao =
        @"
            INSERT INTO transacoes (
                cliente_id, 
                valor, 
                tipo, 
                descricao, 
                realizada_em, 
                versao
            ) VALUES(
                @Id, 
                @Valor, 
                @Tipo, 
                @Descricao, 
                now(), 
                @Versao);
        ";

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
