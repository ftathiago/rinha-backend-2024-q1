namespace RinhaBackend2024Q1.Api.Models.Requests;

public class TransacaoRequest
{
    private string _tipo = "c";

    public int Valor { get; set; }

    public string Tipo
    {
        get => _tipo;
        set => _tipo = value.ToLower();
    }

    public string? Descricao { get; set; }
}
