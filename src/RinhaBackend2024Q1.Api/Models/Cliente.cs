namespace RinhaBackend2024Q1.Api.Models;

public sealed class Cliente
{
    private readonly object _lock = new object();

    private int _saldo;

    public Cliente(int id, int limit, int openingBalance = 0)
    {
        Id = id;
        Limite = limit * -1;
        _saldo = openingBalance;
    }

    public int Id { get; }

    public int Saldo => _saldo;

    public int Limite { get; }

    public void AtualizarSaldo(int value)
    {
        lock (_lock)
        {
            var novoSaldo = _saldo + value;
            if (novoSaldo < Limite)
            {
                return;
            }

            _saldo = novoSaldo;
        }
    }
}
