using System.CommandLine;
using Azure.Identity;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using MetricsPipeline.Core;

namespace MetricsCli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cmd = BuildCommand();
        return await cmd.InvokeAsync(args);
    }

    private static (RootCommand Command,
        Option<string> Ms,
        Option<string> Google,
        Option<string?> Auth,
        Option<string> Output,
        Option<int> Dop,
        Option<bool> Follow) CreateDefinition()
    {
        var msRoot = new Option<string>("--ms-root") { IsRequired = true, Description = "Microsoft root path" };
        var googleRoot = new Option<string>("--google-root") { IsRequired = true, Description = "Google Drive root" };
        var googleAuth = new Option<string?>("--google-auth", description: "Path to Google credentials JSON") { IsRequired = false };
        var output = new Option<string>("--output", () => "mismatches.csv", "CSV output file");
        var maxDop = new Option<int>("--max-dop", () => Environment.ProcessorCount, "Max degree of parallelism");
        var follow = new Option<bool>("--follow-shortcuts", description: "Resolve Google Drive shortcuts");
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
        def.Command.SetHandler(async (string mRoot, string gRoot, string? auth, string outFile, int dop, bool follow) =>
        {
            var options = new Options(mRoot, gRoot, outFile, auth ?? Environment.GetEnvironmentVariable("GOOGLE_AUTH"), dop, follow);
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var googleScanner = CreateGoogleScanner(options, loggerFactory.CreateLogger<GoogleDriveScanner>());
            var msScanner = CreateMicrosoftScanner(options, loggerFactory.CreateLogger<GraphScanner>());
            await using var stream = File.Create(options.Output);
            await PipelineRunner.RunAsync(options, googleScanner, msScanner, stream, loggerFactory);
        }, def.Ms, def.Google, def.Auth, def.Output, def.Dop, def.Follow);
        return def.Command;
    }

    public static Options ParseOptions(string[] args)
    {
        var def = CreateDefinition();
        var result = def.Command.Parse(args);
        var msRoot = result.GetValueForOption(def.Ms)!;
        var googleRoot = result.GetValueForOption(def.Google)!;
        var auth = result.GetValueForOption(def.Auth) ?? Environment.GetEnvironmentVariable("GOOGLE_AUTH");
        var output = result.GetValueForOption(def.Output)!;
        var dop = result.GetValueForOption(def.Dop);
        var follow = result.GetValueForOption(def.Follow);
        return new Options(msRoot, googleRoot, output, auth, dop, follow);
    }

    private static GoogleDriveScanner CreateGoogleScanner(Options options, ILogger<GoogleDriveScanner> logger)
    {
        var authFile = options.GoogleAuth ?? throw new InvalidOperationException("Google credentials missing");
        var credential = GoogleCredential.FromFile(authFile).CreateScoped(DriveService.Scope.DriveReadonly);
        var service = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential });
        return new GoogleDriveScanner(service, logger, followShortcuts: options.FollowShortcuts, maxConcurrency: options.MaxDop);
    }

    private static GraphScanner CreateMicrosoftScanner(Options options, ILogger<GraphScanner> logger)
    {
        var client = new GraphServiceClient(new DefaultAzureCredential());
        return new GraphScanner(client, logger, options.MaxDop);
    }
}

public record Options(string MsRoot, string GoogleRoot, string Output, string? GoogleAuth, int MaxDop, bool FollowShortcuts);
