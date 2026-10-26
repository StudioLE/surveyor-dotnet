using NUnit.Framework;
using Surveyor.Core.Tests.Resources;
using Surveyor.VersionControl;
using Surveyor.Versioning;

namespace Surveyor.Core.Tests.Versioning;

internal sealed class ReleasedVersionProviderTests
{
    private const string Branch = "release";
    private const string Project = "Project";
    private static readonly ReleaseStream _stable = new() { Id = "release", BranchName = Branch };
    private static readonly ReleaseStream _preRelease = new() { Id = "alpha", BranchName = Branch, IsPreRelease = true };

    [Test]
    public void ReleasedVersionProvider_Get_UnchangedSinceTag()
    {
        // Arrange
        using TemporaryGitRepository repo = new(Branch);
        repo.Commit($"{Project}/a.txt");
        repo.Tag("v1.0.0");

        // Act
        SemanticVersion? version = Get(repo, _stable);

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("1.0.0"));
    }

    [Test]
    public void ReleasedVersionProvider_Get_ChangedAfterLatestTag()
    {
        // Arrange
        using TemporaryGitRepository repo = new(Branch);
        repo.Commit($"{Project}/a.txt");
        repo.Tag("v1.0.0");
        repo.Commit($"{Project}/b.txt");

        // Act
        SemanticVersion? version = Get(repo, _stable);

        // Assert
        Assert.That(version, Is.Null);
    }

    [Test]
    public void ReleasedVersionProvider_Get_OtherDirectoryChangedSinceTag()
    {
        // Arrange
        using TemporaryGitRepository repo = new(Branch);
        repo.Commit($"{Project}/a.txt");
        repo.Tag("v1.0.0");
        repo.Commit("Other/b.txt");
        repo.Tag("v1.0.1");
        repo.Commit("Other/c.txt");

        // Act
        SemanticVersion? version = Get(repo, _stable);

        // Assert
        Assert.That(version.ToString(), Is.EqualTo("1.0.0"));
    }

    [TestCase(false, "1.1.0")]
    [TestCase(true, "1.1.0-alpha.1")]
    public void ReleasedVersionProvider_Get_ReleasedAsPreReleaseThenStable(bool isPreRelease, string expected)
    {
        // Arrange
        using TemporaryGitRepository repo = new(Branch);
        repo.Commit($"{Project}/a.txt");
        repo.Tag("v1.0.0");
        repo.Commit($"{Project}/b.txt");
        repo.Tag("v1.1.0-alpha.1");
        repo.Commit("Other/c.txt");
        repo.Tag("v1.1.0");

        // Act
        SemanticVersion? version = Get(repo, isPreRelease ? _preRelease : _stable);

        // Assert
        Assert.That(version.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void ReleasedVersionProvider_Get_NoCommitsInDirectory()
    {
        // Arrange
        using TemporaryGitRepository repo = new(Branch);
        repo.Commit("Other/a.txt");
        repo.Tag("v1.0.0");

        // Act
        SemanticVersion? version = Get(repo, _stable);

        // Assert
        Assert.That(version, Is.Null);
    }

    private static SemanticVersion? Get(TemporaryGitRepository repo, ReleaseStream releaseStream)
    {
        GitCli git = new(new GitCliOptions { Directory = repo.RootDirectory });
        ReleasedVersionProvider provider = new(git);
        string projectDirectory = Path.Combine(repo.RootDirectory, Project);
        return provider.Get(projectDirectory, Branch, releaseStream);
    }
}
