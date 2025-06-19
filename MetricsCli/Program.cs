using System.CommandLine;
using Azure.Identity;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using MetricsPipeline.Core;
using System.Threading;

namespace MetricsCli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cmd = BuildCommand();
        return await cmd.InvokeAsync(args);
    }

    private static (RootCommand Command,
        Option<string?> Ms,
        Option<string?> Google,
        Option<string?> Auth,
        Option<string> Output,
        Option<int> Dop,
        Option<bool> Follow) CreateDefinition()
    {
        var msRoot = new Option<string?>("--ms-root", getDefaultValue: () => Environment.GetEnvironmentVariable("MS_ROOT")) { Description = "Microsoft root path" };
        var googleRoot = new Option<string?>("--google-root", getDefaultValue: () => Environment.GetEnvironmentVariable("GOOGLE_ROOT")) { Description = "Google Drive root" };
        var googleAuth = new Option<string?>("--google-auth", description: "Path to Google credentials JSON") { IsRequired = false };
        var output = new Option<string>("--output", getDefaultValue: () => Environment.GetEnvironmentVariable("OUTPUT_CSV") ?? "mismatches.csv", "CSV output file");
        var maxDop = new Option<int>("--max-dop", getDefaultValue: () =>
            int.TryParse(Environment.GetEnvironmentVariable("MAX_DOP"), out var v) ? v : Environment.ProcessorCount,
            description: "Max degree of parallelism");
        var follow = new Option<bool>("--follow-shortcuts", () => false, "Resolve Google Drive shortcuts");
        var cmd = new RootCommand("Drive mismatch scanning tool");
        cmd.AddOption(msRoot);
        cmd.AddOption(googleRoot);
        cmd.AddOption(googleAuth);
        cmd.AddOption(output);
        cmd.AddOption(maxDop);
        cmd.AddOption(follow);
        return (cmd, msRoot, googleRoot, googleAuth, output, maxDop, follow);
    }

    internal static RootCommand BuildCommand()
    {
        var def = CreateDefinition();
        def.Command.SetHandler(async (string? mRoot, string? gRoot, string? auth, string outFile, int dop, bool follow) =>
        {
            var options = new PipelineOptions(
                mRoot ?? Environment.GetEnvironmentVariable("MS_ROOT") ?? throw new InvalidOperationException("MS root missing"),
                gRoot ?? Environment.GetEnvironmentVariable("GOOGLE_ROOT") ?? throw new InvalidOperationException("Google root missing"),
                outFile,
                auth ?? Environment.GetEnvironmentVariable("GOOGLE_AUTH"),
                dop,
                follow);
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var googleScanner = CreateGoogleScanner(options, loggerFactory.CreateLogger<GoogleDriveScanner>());
            var msScanner = CreateMicrosoftScanner(options, loggerFactory.CreateLogger<GraphScanner>());
            await using var stream = File.Create(options.Output);
            await PipelineRunner.RunAsync(options, googleScanner, msScanner, stream, loggerFactory);
        }, def.Ms, def.Google, def.Auth, def.Output, def.Dop, def.Follow);
        return def.Command;
    }

    public static PipelineOptions ParseOptions(string[] args)
    {
        var def = CreateDefinition();
        var result = def.Command.Parse(args);
        var msRoot = result.GetValueForOption(def.Ms) ?? Environment.GetEnvironmentVariable("MS_ROOT") ?? throw new InvalidOperationException("MS root missing");
        var googleRoot = result.GetValueForOption(def.Google) ?? Environment.GetEnvironmentVariable("GOOGLE_ROOT") ?? throw new InvalidOperationException("Google root missing");
        var auth = result.GetValueForOption(def.Auth) ?? Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        var output = result.GetValueForOption(def.Output);
        var dop = result.GetValueForOption(def.Dop);
        var follow = result.GetValueForOption(def.Follow);
        return new PipelineOptions(msRoot, googleRoot, output!, auth, dop, follow);
    }

    private static GoogleDriveScanner CreateGoogleScanner(PipelineOptions options, ILogger<GoogleDriveScanner> logger)
    {
        var authFile = options.GoogleAuth ?? throw new InvalidOperationException("Google credentials missing");
        var credential = GoogleCredential.FromFile(authFile).CreateScoped(DriveService.Scope.DriveReadonly);
        var service = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential });
        return new GoogleDriveScanner(service, logger, followShortcuts: options.FollowShortcuts, maxConcurrency: options.MaxDop);
    }

    private static GraphScanner CreateMicrosoftScanner(PipelineOptions options, ILogger<GraphScanner> logger)
    {
        var client = new GraphServiceClient(new DefaultAzureCredential());
        return new GraphScanner(client, logger, options.MaxDop);
    }
}

