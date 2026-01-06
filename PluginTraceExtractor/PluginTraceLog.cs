namespace PluginTraceExtractor;

public sealed record PluginTraceLog(
    Guid Id,
    DateTime? CreatedOn,
    string? TypeName,
    string? MessageName,
    string? PrimaryEntity,
    string? MessageBlock,
    string? ExceptionDetails,
    Guid? CorrelationId,
    Guid? RequestId,
    int? Depth,
    int? ExecutionDurationMs,
    int? Mode,
    int? OperationType);

public sealed record PluginTraceLogFilter
{
    public DateTime? CreatedAfterUtc { get; init; }
    public DateTime? CreatedBeforeUtc { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? RequestId { get; init; }
    public string? MessageContains { get; init; }
    public string? PrimaryEntity { get; init; }
    public string? TypeNameContains { get; init; }
    public int? MinExecutionDurationMs { get; init; }
    public int? Top { get; init; }
    public bool ExceptionsOnly { get; init; }
}
