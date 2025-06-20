using System;
using System.IO;
using FluentAssertions;
using Xunit;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests;

public class EnvironmentValidatorTests
{
    [Fact]
    public void Validate_ReturnsTrue_WhenAllVariablesPresent()
    {
        using var tmp = new TempFile();
        var prevMs = Environment.GetEnvironmentVariable("MS_ROOT");
        var prevG = Environment.GetEnvironmentVariable("GOOGLE_ROOT");
        var prevAuth = Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        var prevId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var prevTid = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var prevSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        try
        {
        Environment.SetEnvironmentVariable("MS_ROOT", "m");
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", "g");
        File.WriteAllText(tmp.Path, "{}");
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", tmp.Path);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "id");
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "tid");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "secret");

        EnvironmentValidator.Validate(out var errors).Should().BeTrue();
        errors.Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("MS_ROOT", prevMs);
            Environment.SetEnvironmentVariable("GOOGLE_ROOT", prevG);
            Environment.SetEnvironmentVariable("GOOGLE_AUTH", prevAuth);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", prevId);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", prevTid);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", prevSecret);
        }
    }

    [Fact]
    public void Validate_ReturnsErrors_WhenVariablesMissing()
    {
        var prevMs = Environment.GetEnvironmentVariable("MS_ROOT");
        var prevG = Environment.GetEnvironmentVariable("GOOGLE_ROOT");
        var prevAuth = Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        var prevId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var prevTid = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var prevSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        try
        {
        Environment.SetEnvironmentVariable("MS_ROOT", null);
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", null);
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", null);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", null);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);

        EnvironmentValidator.Validate(out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("MS_ROOT"));
        errors.Should().Contain(e => e.Contains("GOOGLE_ROOT"));
        errors.Should().Contain(e => e.Contains("GOOGLE_AUTH"));
        errors.Should().Contain(e => e.Contains("Azure AD"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MS_ROOT", prevMs);
            Environment.SetEnvironmentVariable("GOOGLE_ROOT", prevG);
            Environment.SetEnvironmentVariable("GOOGLE_AUTH", prevAuth);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", prevId);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", prevTid);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", prevSecret);
        }
    }

    [Fact]
    public void Validate_Fails_WhenCredentialsFileMissing()
    {
        var prevMs = Environment.GetEnvironmentVariable("MS_ROOT");
        var prevG = Environment.GetEnvironmentVariable("GOOGLE_ROOT");
        var prevAuth = Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        var prevId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var prevTid = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var prevSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        try
        {
        Environment.SetEnvironmentVariable("MS_ROOT", "m");
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", "g");
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", Path.Combine(Path.GetTempPath(), "missing.json"));
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "id");
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "tid");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "secret");

        EnvironmentValidator.Validate(out var errors).Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Google credentials not found"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("MS_ROOT", prevMs);
            Environment.SetEnvironmentVariable("GOOGLE_ROOT", prevG);
            Environment.SetEnvironmentVariable("GOOGLE_AUTH", prevAuth);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", prevId);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", prevTid);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", prevSecret);
        }
    }
}

internal sealed class TempFile : IDisposable
{
    public string Path { get; } = System.IO.Path.GetTempFileName();

    public void Dispose()
    {
        if (File.Exists(Path))
            File.Delete(Path);
    }
}
