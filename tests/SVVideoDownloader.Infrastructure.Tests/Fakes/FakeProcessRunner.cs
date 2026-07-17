using System.Collections.Concurrent;
using SVVideoDownloader.Infrastructure.Processes;

namespace SVVideoDownloader.Infrastructure.Tests.Fakes;

internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly ConcurrentQueue<Behavior> _behaviors = new();

    public List<ProcessRequest> Requests { get; } = new();

    public void EnqueueResult(ProcessRunResult result)
    {
        _behaviors.Enqueue(
            async (onOutput, onError, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                EmitLines(result.StandardOutput, onOutput);
                EmitLines(result.StandardError, onError);
                await Task.CompletedTask;
                return result;
            });
    }

    public void EnqueueException(Exception exception)
    {
        _behaviors.Enqueue(
            (_, _, _) => Task.FromException<ProcessRunResult>(exception));
    }

    public void EnqueueWaitForCancellation()
    {
        _behaviors.Enqueue(
            async (_, _, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException();
            });
    }

    public async Task<ProcessRunResult> RunAsync(
        ProcessRequest request,
        Action<string>? onStandardOutputLine = null,
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        if (!_behaviors.TryDequeue(out var behavior))
        {
            throw new InvalidOperationException("No fake process behavior was configured.");
        }

        return await behavior(
            onStandardOutputLine,
            onStandardErrorLine,
            cancellationToken);
    }

    private static void EmitLines(string value, Action<string>? handler)
    {
        if (handler is null)
        {
            return;
        }

        using var reader = new StringReader(value);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            handler(line);
        }
    }

    private delegate Task<ProcessRunResult> Behavior(
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken);
}
