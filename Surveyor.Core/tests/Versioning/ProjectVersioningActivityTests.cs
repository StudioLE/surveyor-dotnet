using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Surveyor.Core.Tests.Resources;
using Surveyor.Hosting;
using Surveyor.VersionControl;
using Surveyor.Versioning;

namespace Surveyor.Core.Tests.Versioning;

internal sealed class ProjectVersioningActivityTests
{
    private readonly IServiceProvider _services;

    public ProjectVersioningActivityTests()
    {
        IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) => services
                .AddSurveyorServices()
                .AddSurveyorOptions())
            .Build();
        _services = host.Services;
    }

    [Test]
    [Explicit("Requires configuration")]
    public async Task ProjectVersioningActivity_Execute_DI()
    {
        // Arrange
        ProjectVersioningActivity activity = _services.GetRequiredService<ProjectVersioningActivity>();
        VersioningActivityOptions options = _services.GetRequiredService<IOptions<VersioningActivityOptions>>().Value;
        options.Branch = "main";

        // Act
        SemanticVersion? version = await activity.Execute(options);

        // Assert
        Assert.That(version, Is.Not.Null);
    }

    [TestCase(ReleaseType.Patch, "release", "1.2.4")]
    [TestCase(ReleaseType.Patch, "main", "1.2.4-alpha.4")]
    [TestCase(ReleaseType.Patch, "beta", "1.2.4-beta.1")]
    [TestCase(ReleaseType.Patch, "rc", "1.2.4-rc.1")]
    [TestCase(ReleaseType.Patch, "wip", null)]
    [TestCase(ReleaseType.Minor, "release", "1.4.0")]
    [TestCase(ReleaseType.Major, "release", "3.0.0")]
    public async Task ProjectVersioningActivity_Execute_UnreleasedChanges(ReleaseType releaseType, string branchName, string? expected)
    {
        // Arrange
        ProjectVersioningActivity activity = CreateActivity(
            releasedVersion: null,
            branchVersions:
            [
                "0.1.0",
                "0.1.1",
                "0.1.2",
                "1.0.0",
                "1.2.3",
                "1.2.4-alpha.1",
                "1.2.4-alpha.2"
            ],
            repositoryVersions:
            [
                "0.1.0",
                "0.1.1",
                "0.1.2",
                "1.0.0",
                "1.2.3",
                "1.2.4-alpha.1",
                "1.2.4-alpha.2",
                "1.2.4-alpha.3",
                "1.3.0",
                "2.0.0",
                "2.1.0"
            ],
            headVersions: [],
            new MockReleaseTypeStrategy(releaseType));

        // Act
        SemanticVersion? version = await activity.Execute(CreateOptions(branchName));

        // Assert
        if (expected is null)
            Assert.That(version, Is.Null);
        else
            Assert.That(version.ToString(), Is.EqualTo(expected));
    }

    [TestCase("release", "1.2.3")]
    [TestCase("main", "1.2.3")]
    [TestCase("wip", null)]
    public async Task ProjectVersioningActivity_Execute_Released(string branchName, string? expected)
    {
        // Arrange
        ProjectVersioningActivity activity = CreateActivity(
            releasedVersion: SemanticVersion.Create("1.2.3"),
            branchVersions: ["1.2.3", "1.3.0"],
            repositoryVersions: ["1.2.3", "1.3.0"],
            headVersions: [],
            new MockReleaseTypeStrategy(ReleaseType.Minor));

        // Act
        SemanticVersion? version = await activity.Execute(CreateOptions(branchName));

        // Assert
        if (expected is null)
            Assert.That(version, Is.Null);
        else
            Assert.That(version.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProjectVersioningActivity_Execute_UnreleasedChanges_HeadTagged()
    {
        // Arrange
        ProjectVersioningActivity activity = CreateActivity(
            releasedVersion: null,
            branchVersions: ["0.1.0", "0.1.1", "0.1.2"],
            repositoryVersions: ["0.1.0", "0.1.1", "0.1.2"],
            headVersions: ["0.1.2"],
            new MockReleaseTypeStrategy(ReleaseType.Patch));

        // Act
        SemanticVersion? version = await activity.Execute(CreateOptions("release"));

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("0.1.2"));
    }

    /// <summary>
    /// A package with unreleased changes gets the repository version,
    /// even if the commits since an older tag would bump it further.
    /// </summary>
    [Test]
    public async Task ProjectVersioningActivity_Execute_UnreleasedChanges_MatchesRepositoryVersion()
    {
        // Arrange
        Dictionary<SemanticVersion, ReleaseType> releaseTypesSince = new()
        {
            { SemanticVersion.Create("0.10.0")!.Value, ReleaseType.Minor },
            { SemanticVersion.Create("0.11.0")!.Value, ReleaseType.Patch }
        };
        ProjectVersioningActivity activity = CreateActivity(
            releasedVersion: null,
            branchVersions: ["0.10.0", "0.11.0"],
            repositoryVersions: ["0.10.0", "0.11.0"],
            headVersions: [],
            new MockReleaseTypeStrategy(ReleaseType.Minor, releaseTypesSince));

        // Act
        SemanticVersion? version = await activity.Execute(CreateOptions("release"));

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("0.11.1"));
    }

    private ProjectVersioningActivity CreateActivity(
        SemanticVersion? releasedVersion,
        string[] branchVersions,
        string[] repositoryVersions,
        string[] headVersions,
        IReleaseTypeStrategy releaseTypeStrategy)
    {
        GitCli git = new(new GitCliOptions
        {
            Directory = Path.GetTempPath(),
            SkipValidation = true
        });
        ReleaseStreamProvider releaseStreamProvider = new();
        RepositoryVersioningActivity repositoryVersioningActivity = new(
            _services.GetRequiredService<ILogger<RepositoryVersioningActivity>>(),
            git,
            new MockRepositoryVersionProvider(CreateVersions(repositoryVersions)),
            new MockBranchVersionProvider(CreateVersions(branchVersions)),
            new MockHeadVersionProvider(CreateVersions(headVersions)),
            releaseTypeStrategy,
            releaseStreamProvider);
        return new(
            _services.GetRequiredService<ILogger<ProjectVersioningActivity>>(),
            git,
            new MockReleasedVersionProvider(releasedVersion),
            releaseStreamProvider,
            repositoryVersioningActivity);
    }

    private static VersioningActivityOptions CreateOptions(string branchName)
    {
        return new()
        {
            Branch = branchName,
            Directory = Path.GetTempPath(),
            Package = "StudioLE.Example"
        };
    }

    private static SemanticVersion[] CreateVersions(string[] versions)
    {
        return versions
            .Select(SemanticVersion.Create)
            .OfType<SemanticVersion>()
            .ToArray();
    }
}
