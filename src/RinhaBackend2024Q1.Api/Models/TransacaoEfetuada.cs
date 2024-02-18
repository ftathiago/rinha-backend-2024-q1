namespace RinhaBackend2024Q1.Api.Models;

public struct TransacaoEfetuada
{
    public int OperationStatus { get; set; }

    public int SaldoAtual { get; set; }

    public int Limite { get; set; }
}
