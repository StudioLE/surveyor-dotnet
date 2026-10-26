using Microsoft.Build.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudioLE.Extensions.Logging.Cache;
using Surveyor.Hosting;
using Surveyor.Versioning;
using Task = Microsoft.Build.Utilities.Task;

namespace Surveyor.BuildVersioning;

/// <summary>
/// A build task to determine the latest version of a NuGet package by querying the package feed.
/// </summary>
/// <seealso href="https://learn.microsoft.com/en-us/visualstudio/msbuild/tutorial-custom-task-code-generation"/>
/// <seealso href="https://learn.microsoft.com/en-us/visualstudio/msbuild/task-writing"/>
public class VersioningTask : Task
{
    /// <summary>
    /// The optional name of the branch.
    /// </summary>
    /// <remarks>
    /// The current git branch will be used if not specified.
    /// </remarks>
    public string Branch { get; set; } = string.Empty;

    /// <summary>
    /// The name of the package.
    /// </summary>
    public string Package { get; set; } = string.Empty;

    /// <summary>
    /// The directory containing the source files for the project.
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the NuGet feed.
    /// </summary>
    /// <remarks>
    /// Ignored. Kept so builds still passing <c>SurveyorFeed</c> don't fail.
    /// </remarks>
    public string Feed { get; set; } = string.Empty;

    /// <summary>
    /// An optional authentication token for the NuGet feed.
    /// </summary>
    /// <remarks>
    /// Ignored. Kept so builds still passing <c>SurveyorToken</c> don't fail.
    /// </remarks>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The version.
    /// </summary>
    [Output]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The version excluding the pre-release component.
    /// </summary>
    [Output]
    public string VersionPrefix { get; set; } = string.Empty;

    /// <summary>
    /// The pre-release component of the version.
    /// </summary>
    [Output]
    public string VersionSuffix { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override bool Execute()
    {
        try
        {
            if (string.IsNullOrEmpty(Package))
            {
                Log.LogError($"{nameof(VersioningTask)} requires {nameof(Package)} to be set.");
                return false;
            }
            if (string.IsNullOrEmpty(Directory))
            {
                Log.LogError($"{nameof(VersioningTask)} requires {nameof(Directory)} to be set.");
                return false;
            }
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureLogging((_, logging) => logging
                    .ClearProviders()
                    .AddCache()
                    .SetMinimumLevel(LogLevel.Debug))
                .ConfigureServices((_, services) => services
                    .AddSurveyorServices()
                    .AddSurveyorOptions())
                .Build();
            ProjectVersioningActivity activity = host.Services.GetRequiredService<ProjectVersioningActivity>();
            VersioningActivityOptions options = new()
            {
                Branch = Branch,
                Package = Package,
                Directory = Directory
            };
            Task<SemanticVersion?> task = activity.Execute(options);
            task.Wait();
            bool hasErrors = ProcessCachedLogs(host.Services);
            if (task.Result is SemanticVersion version)
            {
                SemanticVersion fullVersion = version with
                {
                    PreRelease = string.Empty,
                    Build = string.Empty
                };
                Version = version.ToString();
                VersionPrefix = fullVersion.ToString();
                VersionSuffix = version.PreRelease;
            }
            else
            {
                Version = "0.0.0";
                VersionPrefix = Version;
                VersionSuffix = string.Empty;
            }
            return !hasErrors;
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e);
            return false;
        }
    }

    private bool ProcessCachedLogs(IServiceProvider services)
    {
        IReadOnlyCollection<LogEntry> logs = services.GetCachedLogs();
        foreach (LogEntry log in logs)
            switch (log.LogLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    Log.LogError(log.Message);
                    break;
                case LogLevel.Warning:
                    Log.LogWarning(log.Message);
                    break;
                case LogLevel.Information:
                    Log.LogMessage(MessageImportance.High, log.Message);
                    break;
                case LogLevel.Debug:
                    Log.LogMessage(MessageImportance.Normal, log.Message);
                    break;
                case LogLevel.Trace:
                    Log.LogMessage(MessageImportance.Low, log.Message);
                    break;
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        return logs.Any(x => x.LogLevel is LogLevel.Error or LogLevel.Critical);
    }
}
