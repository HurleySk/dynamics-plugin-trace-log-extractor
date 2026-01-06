using System.Text;

namespace PluginTraceExtractor;

public static class CsvExporter
{
    public static async Task ExportAsync(
        IReadOnlyList<PluginTraceLog> logs,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var writer = new StreamWriter(filePath, append: false, Encoding.UTF8);

        // Write header
        await writer.WriteLineAsync("primaryentity,typename,messagename,messageblock,exceptiondetails").ConfigureAwait(false);

        // Write data rows
        foreach (var log in logs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = string.Join(",",
                EscapeCsvField(log.PrimaryEntity),
                EscapeCsvField(log.TypeName),
                EscapeCsvField(log.MessageName),
                EscapeCsvField(log.MessageBlock),
                EscapeCsvField(log.ExceptionDetails));

            await writer.WriteLineAsync(row).ConfigureAwait(false);
        }
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
