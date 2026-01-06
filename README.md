# Plugin Trace Log Extractor

A .NET console app that extracts Dataverse plugin trace logs to CSV.

## Setup

1. Create `appsettings.json` in the PluginTraceExtractor directory:

```json
{
  "Dataverse": {
    "Url": "https://your-org.crm.dynamics.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

2. Build the project:
```bash
cd PluginTraceExtractor
dotnet build
```

## Usage

```bash
# Export logs from last 24 hours
dotnet run -- --hours 24 --file output.csv

# Export with filters
dotnet run -- --hours 2 --top 100 --exceptions-only --file errors.csv
dotnet run -- --primary-entity pcx_case --hours 12 --file case-logs.csv

# Show help
dotnet run -- --help
```

## Options

| Option | Description |
|--------|-------------|
| `--hours <N>` | Get logs from the last N hours |
| `--top <N>` | Limit to N records |
| `--file <path>` | Output CSV file (default: plugin-trace-export.csv) |
| `--config <path>` | Config file path (default: appsettings.json) |
| `--primary-entity <name>` | Filter by entity name |
| `--type <name>` | Filter by plugin type name (contains) |
| `--correlation <guid>` | Filter by correlation ID |
| `--contains <text>` | Filter by message block content |
| `--min-duration <ms>` | Filter by minimum execution duration |
| `--exceptions-only` | Only include logs with exceptions |

## CSV Columns

- `primaryentity` - The entity that triggered the plugin
- `typename` - Full type name of the plugin
- `messagename` - The message (Create, Update, Delete, etc.)
- `messageblock` - The trace log message content
- `exceptiondetails` - Exception details if any
