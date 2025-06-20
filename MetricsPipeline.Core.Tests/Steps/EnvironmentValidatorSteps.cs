using System;
using System.IO;
using System.Collections.Generic;
using FluentAssertions;
using Reqnroll;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests.Steps;

[Binding]
public class EnvironmentValidatorSteps
{
    private bool _valid;
    private List<string> _errors = null!;
    private string? _credFile;

    private string? _msRoot;
    private string? _googleRoot;
    private string? _googleAuth;
    private string? _clientId;
    private string? _tenantId;
    private string? _secret;

    [BeforeScenario]
    public void Capture()
    {
        _msRoot = Environment.GetEnvironmentVariable("MS_ROOT");
        _googleRoot = Environment.GetEnvironmentVariable("GOOGLE_ROOT");
        _googleAuth = Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        _clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        _tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        _secret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
    }

    [AfterScenario]
    public void Restore()
    {
        Environment.SetEnvironmentVariable("MS_ROOT", _msRoot);
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", _googleRoot);
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", _googleAuth);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", _clientId);
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", _tenantId);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", _secret);
        if (_credFile != null && File.Exists(_credFile))
            File.Delete(_credFile);
    }

    [Given("all required environment variables are set")]
    public void GivenAllVariables()
    {
        _credFile = Path.GetTempFileName();
        File.WriteAllText(_credFile, "{}");
        Environment.SetEnvironmentVariable("MS_ROOT", "m");
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", "g");
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", _credFile);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "id");
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "tid");
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "secret");
    }

    [Given("GOOGLE_AUTH is set to a nonexistent path")]
    public void GivenMissingAuth()
    {
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", Path.Combine(Path.GetTempPath(), "missing.json"));
    }

    [Given("environment variables are cleared")]
    public void GivenVariablesCleared()
    {
        Environment.SetEnvironmentVariable("MS_ROOT", null);
        Environment.SetEnvironmentVariable("GOOGLE_ROOT", null);
        Environment.SetEnvironmentVariable("GOOGLE_AUTH", null);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("AZURE_TENANT_ID", null);
        Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", null);
    }

    [When("I validate the environment")]
    public void WhenIValidate()
    {
        _valid = EnvironmentValidator.Validate(out _errors);
    }

    [Then("validation should succeed")]
    public void ThenSuccess()
    {
        _valid.Should().BeTrue();
        _errors.Should().BeEmpty();
    }

    [Then("validation should fail with message containing \"(.*)\"")]
    public void ThenFail(string part)
    {
        _valid.Should().BeFalse();
        _errors.Should().Contain(e => e.Contains(part));
    }
}
