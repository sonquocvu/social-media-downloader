using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SVVideoDownloader.Infrastructure.Processes;

public sealed class SystemProcessRunner : IProcessRunner
{
    private static readonly TimeSpan TerminationGracePeriod = TimeSpan.FromSeconds(5);

    public async Task<ProcessRunResult> RunAsync(
        ProcessRequest request,
        Action<string>? onStandardOutputLine = null,
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process
        {
            StartInfo = CreateStartInfo(request),
            EnableRaisingEvents = true,
        };

        try
        {
            if (!process.Start())
            {
                throw new ExternalProcessStartException(
                    ProcessStartFailureKind.InvalidExecutable);
            }
        }
        catch (ExternalProcessStartException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new ExternalProcessStartException(
                ProcessStartFailureKind.Inaccessible,
                exception);
        }
        catch (Win32Exception exception)
        {
            throw new ExternalProcessStartException(
                MapStartFailure(exception.NativeErrorCode),
                exception);
        }

        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();
        var callbackFailure = new CallbackFailure();

        var readOutputTask = DrainAsync(
            process.StandardOutput,
            standardOutput,
            request.MaximumCapturedCharacters,
            onStandardOutputLine,
            callbackFailure);
        var readErrorTask = DrainAsync(
            process.StandardError,
            standardError,
            request.MaximumCapturedCharacters,
            onStandardErrorLine,
            callbackFailure);
        var drainTask = Task.WhenAll(readOutputTask, readErrorTask);

        using var timeoutSource = request.Timeout is { } timeout
            ? new CancellationTokenSource(timeout)
            : null;
        using var linkedSource = timeoutSource is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutSource.Token);
        var effectiveToken = linkedSource?.Token ?? cancellationToken;

        using var cancellationRegistration = effectiveToken.Register(
            static state => TryTerminateProcessTree((Process)state!),
            process);

        try
        {
            await process.WaitForExitAsync(effectiveToken).ConfigureAwait(false);
            await drainTask.WaitAsync(effectiveToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (effectiveToken.IsCancellationRequested)
        {
            TryTerminateProcessTree(process);
            await CompleteTerminationAsync(process, drainTask).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            throw new ExternalProcessTimeoutException();
        }

        callbackFailure.ThrowIfCaptured();

        return new ProcessRunResult(
            process.ExitCode,
            standardOutput.ToString(),
            standardError.ToString());
    }

    internal static ProcessStartInfo CreateStartInfo(ProcessRequest request)
    {
        if (ExternalExecutablePolicy.IsBlockedShell(request.ExecutablePath))
        {
            throw new ExternalProcessStartException(
                ProcessStartFailureKind.InvalidExecutable);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = request.ExecutablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in request.ArgumentList)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static async Task DrainAsync(
        StreamReader reader,
        StringBuilder capturedOutput,
        int maximumCapturedCharacters,
        Action<string>? lineHandler,
        CallbackFailure callbackFailure)
    {
        string? line;
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
        {
            AppendWithinLimit(capturedOutput, line, maximumCapturedCharacters);

            if (lineHandler is null)
            {
                continue;
            }

            try
            {
                lineHandler(line);
            }
            catch (Exception exception)
            {
                callbackFailure.Capture(exception);
            }
        }
    }

    private static void AppendWithinLimit(
        StringBuilder builder,
        string line,
        int maximumCapturedCharacters)
    {
        var remaining = maximumCapturedCharacters - builder.Length;
        if (remaining <= 0)
        {
            return;
        }

        var charactersToAppend = Math.Min(line.Length, remaining);
        builder.Append(line, 0, charactersToAppend);

        if (charactersToAppend == line.Length && builder.Length < maximumCapturedCharacters)
        {
            builder.AppendLine();
        }
    }

    private static ProcessStartFailureKind MapStartFailure(int nativeErrorCode) =>
        nativeErrorCode switch
        {
            2 or 3 => ProcessStartFailureKind.Missing,
            5 or 740 => ProcessStartFailureKind.Inaccessible,
            193 or 216 => ProcessStartFailureKind.InvalidExecutable,
            _ => ProcessStartFailureKind.Unknown,
        };

    private static void TryTerminateProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    private static async Task CompleteTerminationAsync(Process process, Task drainTask)
    {
        try
        {
            await process
                .WaitForExitAsync(CancellationToken.None)
                .WaitAsync(TerminationGracePeriod)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
        }
        catch (TimeoutException)
        {
        }

        if (!drainTask.IsCompleted)
        {
            TryClose(process.StandardOutput);
            TryClose(process.StandardError);
        }

        try
        {
            await drainTask.WaitAsync(TerminationGracePeriod).ConfigureAwait(false);
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (TimeoutException)
        {
        }
    }

    private static void TryClose(StreamReader reader)
    {
        try
        {
            reader.Close();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private sealed class CallbackFailure
    {
        private Exception? _exception;

        public void Capture(Exception exception) =>
            Interlocked.CompareExchange(ref _exception, exception, null);

        public void ThrowIfCaptured()
        {
            if (_exception is not null)
            {
                throw _exception;
            }
        }
    }
}
