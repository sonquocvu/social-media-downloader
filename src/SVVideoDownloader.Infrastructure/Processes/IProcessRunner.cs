using System;
using System.Threading;
using System.Threading.Tasks;

namespace SVVideoDownloader.Infrastructure.Processes;

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        ProcessRequest request,
        Action<string>? onStandardOutputLine = null,
        Action<string>? onStandardErrorLine = null,
        CancellationToken cancellationToken = default);
}
