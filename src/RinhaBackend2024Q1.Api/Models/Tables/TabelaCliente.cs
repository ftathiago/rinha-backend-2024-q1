namespace RinhaBackend2024Q1.Api.Models.Tables;

public class TabelaCliente
{
    public int Id { get; set; }

    public string? Nome { get; set; }

    public int Limite { get; set; }

    public int SaldoAtual { get; set; }

    public int Versao { get; set; }
}
