using RinhaBackend2024Q1.Api.Models.Responses;

namespace RinhaBackend2024Q1.Api.Models.Tables;

public class ViewExtrato
{
    public ViewExtrato()
    {
        UltimasTransacoes = new List<TransacaoResponse>(10);
    }

    public int Total { get; set; }

    public DateTime DataExtrato { get; set; } = DateTime.Now;

    public int Limite { get; set; }

    public List<TransacaoResponse> UltimasTransacoes { get; }
}
