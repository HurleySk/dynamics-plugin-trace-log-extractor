using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace PluginTraceExtractor;

public sealed class DataverseClient : IDisposable
{
    private readonly ServiceClient _client;

    public DataverseClient(DataverseConfig config)
    {
        var connectionString = $"AuthType=ClientSecret;Url={config.Url};ClientId={config.ClientId};ClientSecret={config.ClientSecret};TenantId={config.TenantId};LoginPrompt=Never;";
        _client = new ServiceClient(connectionString);

        if (!_client.IsReady)
        {
            throw new InvalidOperationException($"Failed to connect to Dataverse: {_client.LastError}");
        }
    }

    public async Task<IReadOnlyList<PluginTraceLog>> GetPluginTraceLogsAsync(
        PluginTraceLogFilter? filter,
        CancellationToken cancellationToken = default)
    {
        filter ??= new PluginTraceLogFilter();

        var query = new QueryExpression("plugintracelog")
        {
            ColumnSet = new ColumnSet(
                "plugintracelogid",
                "createdon",
                "typename",
                "messagename",
                "primaryentity",
                "messageblock",
                "exceptiondetails",
                "correlationid",
                "requestid",
                "depth",
                "performanceexecutionduration",
                "mode",
                "operationtype"),
            Orders =
            {
                new OrderExpression("createdon", OrderType.Descending)
            }
        };

        if (filter.Top.HasValue && filter.Top.Value > 0)
        {
            query.TopCount = filter.Top.Value;
        }

        if (filter.CreatedAfterUtc.HasValue)
        {
            query.Criteria.AddCondition("createdon", ConditionOperator.OnOrAfter, filter.CreatedAfterUtc.Value);
        }

        if (filter.CreatedBeforeUtc.HasValue)
        {
            query.Criteria.AddCondition("createdon", ConditionOperator.OnOrBefore, filter.CreatedBeforeUtc.Value);
        }

        if (filter.CorrelationId.HasValue)
        {
            query.Criteria.AddCondition("correlationid", ConditionOperator.Equal, filter.CorrelationId.Value);
        }

        if (filter.RequestId.HasValue)
        {
            query.Criteria.AddCondition("requestid", ConditionOperator.Equal, filter.RequestId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.PrimaryEntity))
        {
            query.Criteria.AddCondition("primaryentity", ConditionOperator.Equal, filter.PrimaryEntity.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.TypeNameContains))
        {
            query.Criteria.AddCondition("typename", ConditionOperator.Like, $"%{EscapeLikeValue(filter.TypeNameContains)}%");
        }

        if (!string.IsNullOrWhiteSpace(filter.MessageContains))
        {
            query.Criteria.AddCondition("messageblock", ConditionOperator.Like, $"%{EscapeLikeValue(filter.MessageContains)}%");
        }

        if (filter.MinExecutionDurationMs.HasValue && filter.MinExecutionDurationMs.Value > 0)
        {
            query.Criteria.AddCondition("performanceexecutionduration", ConditionOperator.GreaterEqual, filter.MinExecutionDurationMs.Value);
        }

        if (filter.ExceptionsOnly)
        {
            query.Criteria.AddCondition("exceptiondetails", ConditionOperator.NotNull);
            query.Criteria.AddCondition("exceptiondetails", ConditionOperator.NotEqual, string.Empty);
        }

        var collection = await Task.Run(() => _client.RetrieveMultiple(query), cancellationToken).ConfigureAwait(false);
        return collection.Entities.Select(ConvertToPluginTraceLog).ToList();
    }

    private static PluginTraceLog ConvertToPluginTraceLog(Entity entity)
    {
        return new PluginTraceLog(
            entity.Id,
            entity.GetAttributeValue<DateTime?>("createdon"),
            entity.GetAttributeValue<string>("typename"),
            entity.GetAttributeValue<string>("messagename"),
            entity.GetAttributeValue<string>("primaryentity"),
            entity.GetAttributeValue<string>("messageblock"),
            entity.GetAttributeValue<string>("exceptiondetails"),
            entity.GetAttributeValue<Guid?>("correlationid"),
            entity.GetAttributeValue<Guid?>("requestid"),
            entity.GetAttributeValue<int?>("depth"),
            entity.GetAttributeValue<int?>("performanceexecutionduration"),
            entity.GetAttributeValue<OptionSetValue>("mode")?.Value,
            entity.GetAttributeValue<OptionSetValue>("operationtype")?.Value);
    }

    private static string EscapeLikeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}

public sealed class DataverseConfig
{
    public string Url { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
