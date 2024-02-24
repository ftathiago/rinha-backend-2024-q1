using System.Text.Json.Serialization;

namespace RinhaBackend2024Q1.Api.Models.Responses;

public class Extrato
{
    public Extrato()
    {
        Ultimas_Transacoes = new Queue<TransacaoResponse>(10);
    }

    public Saldo? Saldo { get; set; }

    [JsonPropertyName("ultimas_transacoes")]
    public Queue<TransacaoResponse> Ultimas_Transacoes { get; set; }
}
