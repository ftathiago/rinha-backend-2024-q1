namespace RinhaBackend2024Q1.Api.Models.Responses;

public class Extrato
{
    public Extrato()
    {
        UltimasTransacoes = new List<TransacaoResponse>(10);
    }

    public Saldo? Saldo { get; set; }

    public List<TransacaoResponse> UltimasTransacoes { get; set; }
}
