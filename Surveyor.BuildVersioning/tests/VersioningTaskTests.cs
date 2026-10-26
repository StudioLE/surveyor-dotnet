using Microsoft.Build.Framework;
using NUnit.Framework;
using Surveyor.BuildVersioning.Tests.Resources;

namespace Surveyor.BuildVersioning.Tests;

internal sealed class VersioningTaskTests
{
    [Test]
    [Explicit("Requires main branch")]
    public void VersioningTask_Execute()
    {
        // Arrange
        MockBuildEngine engine = new();
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "../../../../../Surveyor.Core/src");
        directory = Path.GetFullPath(directory);
        Environment.SetEnvironmentVariable("GIT__DIRECTORY", directory);
        VersioningTask task = new()
        {
            BuildEngine = engine,
            Package = "Surveyor.Core",
            Directory = directory,
            Branch = "main"
        };

        // Act
        bool result = task.Execute();

        // Preview
        Console.WriteLine($"Version: {task.Version}");
        WriteLogs("Errors", engine.Errors);
        WriteLogs("Warnings", engine.Warnings);
        WriteLogs("Messages", engine.Messages);
        WriteLogs("CustomEvents", engine.CustomEvents);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result);
            Assert.That(engine.Errors.Count, Is.Zero);
            Assert.That(engine.Warnings.Count, Is.Zero);
            Assert.That(engine.Messages.Count, Is.EqualTo(3));
            Assert.That(engine.CustomEvents.Count, Is.Zero);
            Assert.That(task.Version, Is.Not.Empty);
        });
    }

    private static void WriteLogs(string logType, IReadOnlyCollection<LazyFormattedBuildEventArgs> e)
    {
        if(e.Count == 0)
            return;
        Console.WriteLine($"{logType}:");
        foreach (LazyFormattedBuildEventArgs args in e)
            Console.WriteLine(args.Message);
    }
}
