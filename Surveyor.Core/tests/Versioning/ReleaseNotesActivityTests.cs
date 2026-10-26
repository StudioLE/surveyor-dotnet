using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Surveyor.Hosting;
using Surveyor.ReleaseNotes;

namespace Surveyor.Core.Tests.Versioning;

internal sealed class ReleaseNotesActivityTests
{
    private readonly IServiceProvider _services;

    public ReleaseNotesActivityTests()
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
    public void ReleaseNotesActivity_Execute_DI()
    {
        // Arrange
        ReleaseNotesActivity activity = _services.GetRequiredService<ReleaseNotesActivity>();
        ReleaseNotesActivityOptions options = new()
        {
            Branch = "main"
        };

        // Act
        string notes = activity.Execute(options);

        // Assert
        Assert.That(notes, Is.Not.Empty);
    }
}
