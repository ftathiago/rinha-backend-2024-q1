namespace RinhaBackend2024Q1.Api.Models.Responses;

public class Extrato
{
    public Extrato()
    {
        UltimasTransacoes = new Queue<TransacaoResponse>(10);
    }

    public Saldo? Saldo { get; set; }

    public Queue<TransacaoResponse> UltimasTransacoes { get; set; }
}
