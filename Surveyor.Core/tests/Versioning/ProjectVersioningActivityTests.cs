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
    [TestCase(ReleaseType.Patch, "alpha", "1.2.4-alpha.4")]
    [TestCase(ReleaseType.Patch, "beta", "1.2.4-beta.1")]
    [TestCase(ReleaseType.Patch, "rc", "1.2.4-rc.1")]
    // [TestCase(ReleaseType.Patch, "v2", null)]
    // [TestCase(ReleaseType.Patch, "v2.2", null)]
    // [TestCase(ReleaseType.Patch, "v3", "3.0.0")]
    [TestCase(ReleaseType.Patch, "wip", null)]
    [TestCase(ReleaseType.Minor, "release", "1.4.0")]
    [TestCase(ReleaseType.Major, "release", "3.0.0")]
    public async Task ProjectVersioningActivity_Execute(ReleaseType releaseType, string branchName, string? expected)
    {
        // Arrange
        ILogger<ProjectVersioningActivity> logger = _services.GetRequiredService<ILogger<ProjectVersioningActivity>>();
        MockPublishedVersionProvider publishedVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            // "0.1.2",
            "1.0.0",
            "1.2.3",
            "1.2.4-alpha.1",
            "1.2.4-alpha.2",
            // "1.2.4-alpha.3",
            // "1.3.0",
            "2.0.0"
            // "2.1.0"
        ]));
        MockRepositoryVersionProvider repositoryVersionProvider = new(CreateVersions([
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
        ]));
        MockBranchVersionProvider branchVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2",
            "1.0.0",
            "1.2.3",
            "1.2.4-alpha.1",
            "1.2.4-alpha.2"
            // "1.2.4-alpha.3"
            // "1.3.0",
            // "2.0.0",
            // "2.1.0"
        ]));
        MockHeadVersionProvider headVersionProvider = new([]);
        MockChangedFileProvider changedFileProvider = new([
            "Surveyor.Core/Versioning/VersioningActivity.cs"
        ]);
        ReleaseStreamProvider releaseStreamProvider = new();
        MockReleaseTypeStrategy releaseTypeStrategy = new(releaseType);
        VersioningActivityOptions options = _services.GetRequiredService<IOptions<VersioningActivityOptions>>().Value;
        options.Branch = branchName;
        options.Directory = Path.GetTempPath();
        options.Package = "StudioLE.Example";
        GitCli git = new(new GitCliOptions
        {
            Directory = options.Directory,
            SkipValidation = true
        });
        ProjectVersioningActivity activity = new(
            logger,
            git,
            publishedVersionProvider,
            repositoryVersionProvider,
            branchVersionProvider,
            headVersionProvider,
            changedFileProvider,
            releaseTypeStrategy,
            releaseStreamProvider);

        // Act
        SemanticVersion? version = await activity.Execute(options);

        // Assert
        if (expected is null)
            Assert.That(version, Is.Null);
        else
            Assert.That(version.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task ProjectVersioningActivity_Execute_NoProjectChanges_HeadNotTagged()
    {
        // Arrange
        ILogger<ProjectVersioningActivity> logger = _services.GetRequiredService<ILogger<ProjectVersioningActivity>>();
        MockPublishedVersionProvider publishedVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1"
            // "0.1.2"
        ]));
        MockRepositoryVersionProvider repositoryVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2"
        ]));
        MockBranchVersionProvider branchVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2"
        ]));
        MockHeadVersionProvider headVersionProvider = new([]);
        MockChangedFileProvider changedFileProvider = new([]);
        ReleaseStreamProvider releaseStreamProvider = new();
        MockReleaseTypeStrategy releaseTypeStrategy = new(ReleaseType.Patch);
        VersioningActivityOptions options = _services.GetRequiredService<IOptions<VersioningActivityOptions>>().Value;
        options.Branch = "alpha";
        options.Directory = Path.GetTempPath();
        options.Package = "StudioLE.Example";
        GitCli git = new(new GitCliOptions
        {
            Directory = options.Directory,
            SkipValidation = true
        });
        ProjectVersioningActivity activity = new(
            logger,
            git,
            publishedVersionProvider,
            repositoryVersionProvider,
            branchVersionProvider,
            headVersionProvider,
            changedFileProvider,
            releaseTypeStrategy,
            releaseStreamProvider);

        // Act
        SemanticVersion? version = await activity.Execute(options);

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("0.1.1"));
    }

    [Test]
    public async Task VersioningActivity_Execute_HeadTaggedButNotPublished()
    {
        // Arrange
        ILogger<ProjectVersioningActivity> logger = _services.GetRequiredService<ILogger<ProjectVersioningActivity>>();
        MockPublishedVersionProvider publishedVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1"
            // "0.1.2"
        ]));
        MockRepositoryVersionProvider repositoryVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2"
        ]));
        MockBranchVersionProvider branchVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2"
        ]));
        MockHeadVersionProvider headVersionProvider = new(CreateVersions([
            "0.1.2"
        ]));
        MockChangedFileProvider changedFileProvider = new([
            "Surveyor.Core/Versioning/VersioningActivity.cs"
        ]);
        ReleaseStreamProvider releaseStreamProvider = new();
        MockReleaseTypeStrategy releaseTypeStrategy = new(ReleaseType.Patch);
        VersioningActivityOptions options = _services.GetRequiredService<IOptions<VersioningActivityOptions>>().Value;
        options.Branch = "release";
        options.Directory = Path.GetTempPath();
        options.Package = "StudioLE.Example";
        GitCli git = new(new GitCliOptions
        {
            Directory = options.Directory,
            SkipValidation = true
        });
        ProjectVersioningActivity activity = new(
            logger,
            git,
            publishedVersionProvider,
            repositoryVersionProvider,
            branchVersionProvider,
            headVersionProvider,
            changedFileProvider,
            releaseTypeStrategy,
            releaseStreamProvider);

        // Act
        SemanticVersion? version = await activity.Execute(options);

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("0.1.2"));
    }

    [Test]
    public async Task VersioningActivity_Execute_NoProjectChanges_HeadTaggedPreRelease()
    {
        // Arrange
        ILogger<ProjectVersioningActivity> logger = _services.GetRequiredService<ILogger<ProjectVersioningActivity>>();
        MockPublishedVersionProvider publishedVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1"
        ]));
        MockRepositoryVersionProvider repositoryVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2-alpha.1"
        ]));
        MockBranchVersionProvider branchVersionProvider = new(CreateVersions([
            "0.1.0",
            "0.1.1",
            "0.1.2-alpha.1"
        ]));
        MockHeadVersionProvider headVersionProvider = new(CreateVersions([
            "0.1.2-alpha.1"
        ]));
        MockChangedFileProvider changedFileProvider = new([]);
        ReleaseStreamProvider releaseStreamProvider = new();
        MockReleaseTypeStrategy releaseTypeStrategy = new(ReleaseType.Patch);
        VersioningActivityOptions options = _services.GetRequiredService<IOptions<VersioningActivityOptions>>().Value;
        options.Branch = "main";
        options.Directory = Path.GetTempPath();
        options.Package = "StudioLE.Example";
        GitCli git = new(new GitCliOptions
        {
            Directory = options.Directory,
            SkipValidation = true
        });
        ProjectVersioningActivity activity = new(
            logger,
            git,
            publishedVersionProvider,
            repositoryVersionProvider,
            branchVersionProvider,
            headVersionProvider,
            changedFileProvider,
            releaseTypeStrategy,
            releaseStreamProvider);

        // Act
        SemanticVersion? version = await activity.Execute(options);

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("0.1.1"));
    }

    private static SemanticVersion[] CreateVersions(string[] versions)
    {
        return versions
            .Select(SemanticVersion.Create)
            .OfType<SemanticVersion>()
            .ToArray();
    }
}
