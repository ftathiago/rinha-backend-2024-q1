namespace RinhaBackend2024Q1.Api.Models.Tables;

public class ViewExtrato
{
    public int Total { get; set; }

    public DateTime DataExtrato { get; set; } = DateTime.Now;

    public int Limite { get; set; }

    public int Valor { get; set; }

    public int? TransacaoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public DateTime? RealizadaEm { get; set; }
}
