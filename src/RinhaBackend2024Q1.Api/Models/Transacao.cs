namespace RinhaBackend2024Q1.Api.Models;

public class Transacao
{
    public int Valor { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public DateTime? RealizadaEm { get; set; }
}
