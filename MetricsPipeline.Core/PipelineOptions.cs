namespace MetricsPipeline.Core;

/// <summary>
/// Options controlling the end-to-end pipeline execution.
/// </summary>
public record PipelineOptions(string MsRoot, string GoogleRoot, string Output, string? GoogleAuth, int MaxDop = 4, bool FollowShortcuts = false);
