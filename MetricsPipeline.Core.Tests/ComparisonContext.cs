using Moq;
using MetricsPipeline.Core;

namespace MetricsPipeline.Core.Tests;

public sealed class ComparisonContext
{
    public Mock<IGoogleScanner> GoogleMock { get; } = new(MockBehavior.Strict);
    public Mock<IMicrosoftScanner> MicrosoftMock { get; } = new(MockBehavior.Strict);
}
