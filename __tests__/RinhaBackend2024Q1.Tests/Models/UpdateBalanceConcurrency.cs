using RinhaBackend2024Q1.Api.Models;


namespace RinhaBackend2024Q1.Tests.Models;

public class UpdateBalanceConcurrency
{
    [Fact]
    public void Test1()
    {
        var performance = new Performance();
        const int Expected = 500500;

        performance.ExecutionTimeOf(perf => perf.Execute()).Should().BeLessThanOrEqualTo(4.Milliseconds());
        performance.Client.Saldo.Should().Be(Expected);
    }
}

public class Performance
{
    private readonly List<Task> _task;
    public Performance()
    {
        Client = new Cliente(
            id: 1,
            limit: 1000,
            openingBalance: 0);

        _task = Enumerable
            .Range(1, 1000)
            .Select(index => Task.Run(() => Client.AtualizarSaldo(index)))
            .ToList();
    }

    public void Execute()
    {
        Task.WhenAll(_task).ConfigureAwait(false);
    }

    public Cliente Client { get; }
}
