using System.Globalization;
using Microsoft.Extensions.Configuration;
using PluginTraceExtractor;

// Parse command line arguments (args is implicitly available in top-level statements)
var hoursArg = GetArg(args, "--hours");
var topArg = GetArg(args, "--top");
var fileArg = GetArg(args, "--file") ?? "plugin-trace-export.csv";
var configArg = GetArg(args, "--config") ?? "appsettings.json";
var primaryEntityArg = GetArg(args, "--primary-entity");
var typeArg = GetArg(args, "--type");
var correlationArg = GetArg(args, "--correlation");
var containsArg = GetArg(args, "--contains") ?? GetArg(args, "--message");
var minDurationArg = GetArg(args, "--min-duration");
var exceptionsOnly = HasSwitch(args, "--exceptions-only");
var showHelp = HasSwitch(args, "--help") || HasSwitch(args, "-h");

if (showHelp)
{
    PrintHelp();
    return 0;
}

// Load configuration
if (!File.Exists(configArg))
{
    Console.Error.WriteLine($"Configuration file not found: {configArg}");
    Console.Error.WriteLine("Create an appsettings.json with your Dataverse connection details.");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(configArg, optional: false)
    .Build();

var dataverseConfig = new DataverseConfig();
configuration.GetSection("Dataverse").Bind(dataverseConfig);

if (string.IsNullOrWhiteSpace(dataverseConfig.Url))
{
    Console.Error.WriteLine("Dataverse:Url is required in configuration.");
    return 1;
}

// Build filter
var filter = new PluginTraceLogFilter
{
    Top = ParseInt(topArg),
    PrimaryEntity = primaryEntityArg,
    TypeNameContains = typeArg,
    CorrelationId = ParseGuid(correlationArg),
    MessageContains = containsArg,
    MinExecutionDurationMs = ParseInt(minDurationArg),
    ExceptionsOnly = exceptionsOnly
};

// Parse hours filter
if (!string.IsNullOrWhiteSpace(hoursArg) &&
    double.TryParse(hoursArg, NumberStyles.Float, CultureInfo.InvariantCulture, out var hours) &&
    hours > 0)
{
    filter = filter with { CreatedAfterUtc = DateTime.UtcNow.AddHours(-hours) };
}

// Connect and query
Console.WriteLine($"Connecting to Dataverse: {dataverseConfig.Url}");

try
{
    using var client = new DataverseClient(dataverseConfig);

    Console.WriteLine("Querying plugin trace logs...");
    var logs = await client.GetPluginTraceLogsAsync(filter).ConfigureAwait(false);

    Console.WriteLine($"Found {logs.Count} log(s). Exporting to {fileArg}...");
    await CsvExporter.ExportAsync(logs, fileArg).ConfigureAwait(false);

    Console.WriteLine($"Successfully exported {logs.Count} plugin trace log(s) to {fileArg}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

// Helper methods
static string? GetArg(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}

static bool HasSwitch(string[] args, string name)
{
    return args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
}

static int? ParseInt(string? value)
{
    if (!string.IsNullOrWhiteSpace(value) &&
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
    {
        return result;
    }
    return null;
}

static Guid? ParseGuid(string? value)
{
    if (!string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out var result))
    {
        return result;
    }
    return null;
}

static void PrintHelp()
{
    Console.WriteLine("""
        Plugin Trace Log Extractor
        Extracts Dataverse plugin trace logs to CSV.

        Usage:
          PluginTraceExtractor [options]

        Options:
          --hours <N>            Get logs from the last N hours
          --top <N>              Limit to N records (default: all)
          --file <path>          Output CSV file (default: plugin-trace-export.csv)
          --config <path>        Config file path (default: appsettings.json)
          --primary-entity <name> Filter by entity name
          --type <name>          Filter by plugin type name (contains)
          --correlation <guid>   Filter by correlation ID
          --contains <text>      Filter by message block content
          --min-duration <ms>    Filter by minimum execution duration
          --exceptions-only      Only include logs with exceptions
          --help, -h             Show this help

        Examples:
          PluginTraceExtractor --hours 24 --file output.csv
          PluginTraceExtractor --hours 2 --top 100 --exceptions-only
          PluginTraceExtractor --primary-entity pcx_case --hours 12

        Configuration (appsettings.json):
          {
            "Dataverse": {
              "Url": "https://your-org.crm.dynamics.com/",
              "TenantId": "your-tenant-id",
              "ClientId": "your-client-id",
              "ClientSecret": "your-client-secret"
            }
          }
        """);
}
