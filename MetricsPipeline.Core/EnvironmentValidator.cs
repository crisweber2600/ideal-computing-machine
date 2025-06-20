using System;
using System.Collections.Generic;
using System.IO;

namespace MetricsPipeline.Core;

/// <summary>
/// Validates that required environment variables and credentials exist before running the pipeline.
/// </summary>
public static class EnvironmentValidator
{
    /// <summary>
    /// Checks environment variables used by the scanners and collects error messages.
    /// </summary>
    /// <param name="errors">Populated with error descriptions when validation fails.</param>
    /// <returns><c>true</c> when the environment is correctly configured.</returns>
    public static bool Validate(out List<string> errors)
    {
        errors = new List<string>();

        var msRoot = Environment.GetEnvironmentVariable("MS_ROOT");
        if (string.IsNullOrWhiteSpace(msRoot))
            errors.Add("MS_ROOT is not set.");

        var googleRoot = Environment.GetEnvironmentVariable("GOOGLE_ROOT");
        if (string.IsNullOrWhiteSpace(googleRoot))
            errors.Add("GOOGLE_ROOT is not set.");

        var auth = Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        if (string.IsNullOrWhiteSpace(auth))
        {
            errors.Add("GOOGLE_AUTH is not set.");
        }
        else if (!File.Exists(auth))
        {
            errors.Add($"Google credentials not found: {auth}");
        }

        var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
        var secret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(secret))
            errors.Add("Azure AD client credentials are missing.");

        return errors.Count == 0;
    }
}
