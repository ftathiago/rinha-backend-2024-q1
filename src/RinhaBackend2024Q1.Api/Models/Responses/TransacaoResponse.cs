namespace RinhaBackend2024Q1.Api.Models.Responses;

public struct TransacaoResponse
{
    public int Valor { get; set; }

    public string Tipo { get; set; }

    public string Descricao { get; set; }

    public DateTime? RealizadaEm { get; set; }
}
