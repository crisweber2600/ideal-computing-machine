namespace MetricsPipeline.Core;

/// <summary>
/// Options controlling the end-to-end pipeline execution.
/// </summary>
public record class PipelineOptions
{
    /// <summary>
    /// Parameterless constructor required for configuration binding.
    /// Initializes all string properties to empty values.
    /// </summary>
    public PipelineOptions() : this(string.Empty, string.Empty, string.Empty, null) { }

    public PipelineOptions(string msRoot, string googleRoot, string output, string? googleAuth, int maxDop = 4, bool followShortcuts = false)
    {
        MsRoot = msRoot;
        GoogleRoot = googleRoot;
        Output = output;
        GoogleAuth = googleAuth;
        MaxDop = maxDop;
        FollowShortcuts = followShortcuts;
    }

    public string MsRoot { get; init; } = string.Empty;

    public string GoogleRoot { get; init; } = string.Empty;

    public string Output { get; init; } = string.Empty;

    public string? GoogleAuth { get; init; }

    public int MaxDop { get; init; } = 4;

    public bool FollowShortcuts { get; init; }
}
