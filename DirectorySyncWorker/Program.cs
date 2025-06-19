using DirectorySyncWorker;
using Azure.Identity;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Graph;
using MetricsPipeline.Core;
using MetricsPipeline.Core.Infrastructure.Workers;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<PipelineOptions>(builder.Configuration.GetSection("Pipeline"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<PipelineOptions>>().Value);
builder.Services.AddSingleton<ILoggerFactory>(sp => LoggerFactory.Create(b => b.AddConsole()));
builder.Services.AddSingleton<GoogleDriveScanner>(sp =>
{
    var opts = sp.GetRequiredService<PipelineOptions>();
    var credential = GoogleCredential.FromFile(opts.GoogleAuth!).CreateScoped(DriveService.Scope.DriveReadonly);
    var service = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential });
    var logger = sp.GetRequiredService<ILogger<GoogleDriveScanner>>();
    return new GoogleDriveScanner(service, logger, opts.FollowShortcuts, opts.MaxDop);
});
builder.Services.AddSingleton<GraphScanner>(sp =>
{
    var opts = sp.GetRequiredService<PipelineOptions>();
    var client = new GraphServiceClient(new DefaultAzureCredential());
    var logger = sp.GetRequiredService<ILogger<GraphScanner>>();
    return new GraphScanner(client, logger, opts.MaxDop);
});
builder.Services.AddHostedService<PipelineWorker>();

var host = builder.Build();
host.Run();
