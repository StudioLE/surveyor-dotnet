using NUnit.Framework;
using Surveyor.VersionControl;

namespace Surveyor.Core.Tests.Versioning;

[TestFixture]
internal class GitCliValidationTests
{
    [TestCase("main")]
    [TestCase("develop")]
    [TestCase("release")]
    [TestCase("v1.0.0")]
    [TestCase("v0.0.1")]
    [TestCase("feature/add-login")]
    [TestCase("fix/123-bug")]
    [TestCase("release/1.0")]
    [TestCase("refs/heads/main")]
    [TestCase("abc123def")]
    [TestCase("feature/issue-42")]
    [TestCase("my.branch.name")]
    [TestCase("my_branch_name")]
    [TestCase("my-branch-name")]
    [TestCase("Feature/Add-Login")]
    [TestCase("RELEASE")]
    [TestCase("feature/#123-fix")]
    [TestCase("issue#42")]
    [TestCase("user@feature")]
    [TestCase("release@2")]
    [TestCase("feature+fix")]
    [TestCase("v1.0.0+build.123")]
    [TestCase("feature%2Ffix")]
    public void ValidateGitReference_Valid(string reference)
    {
        bool result = GitCli.ValidateGitReferenceArgument(reference);
        Assert.That(result, Is.True);
    }

    [TestCase("")]
    [TestCase("-starts-with-hyphen")]
    [TestCase(".starts-with-dot")]
    [TestCase("has spaces")]
    [TestCase("has\"quotes")]
    [TestCase("has\nnewline")]
    [TestCase("has\ttab")]
    [TestCase("has~tilde")]
    [TestCase("has^caret")]
    [TestCase("has:colon")]
    [TestCase("has?question")]
    [TestCase("has*star")]
    [TestCase("has[bracket")]
    [TestCase("has\\backslash")]
    [TestCase("has`backtick")]
    [TestCase("has$dollar")]
    [TestCase("has!bang")]
    [TestCase("has;semicolon")]
    [TestCase("has&ampersand")]
    [TestCase("has|pipe")]
    [TestCase("has<angle")]
    [TestCase("has>angle")]
    [TestCase("has(paren")]
    [TestCase("has)paren")]
    public void ValidateGitReference_Invalid(string reference)
    {
        bool result = GitCli.ValidateGitReferenceArgument(reference);
        Assert.That(result, Is.False);
    }

    [TestCase("/var/mnt/e/Repos")]
    [TestCase("C:/Users/name/repos")]
    [TestCase("/home/user/my project")]
    [TestCase("path/to/repo")]
    [TestCase("path-with-hyphens/and_underscores")]
    [TestCase("path.with.dots")]
    [TestCase("C:/Repos/C#/Surveyor")]
    public void ValidatePathArgument_Valid(string path)
    {
        bool result = GitCli.ValidatePathArgument(path);
        Assert.That(result, Is.True);
    }

    [TestCase("")]
    [TestCase("path\"with\"quotes")]
    [TestCase("path\nwith\nnewlines")]
    [TestCase("path`with`backticks")]
    [TestCase("path$with$dollar")]
    public void ValidatePathArgument_Invalid(string path)
    {
        bool result = GitCli.ValidatePathArgument(path);
        Assert.That(result, Is.False);
    }
}
