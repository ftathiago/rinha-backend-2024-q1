using System.Text.Json.Serialization;

namespace RinhaBackend2024Q1.Api.Models.Responses;

public class Saldo
{
    public int Total { get; set; }

    [JsonPropertyName("data_extrato")]
    public DateTime DataExtrato { get; set; }

    public int Limite { get; set; }
}
