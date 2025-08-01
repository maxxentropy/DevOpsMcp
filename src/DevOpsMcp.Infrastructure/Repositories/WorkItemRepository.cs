using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using DevOpsMcp.Infrastructure.Services;
using DomainWorkItem = DevOpsMcp.Domain.Entities.WorkItem;
using DomainWorkItemRelation = DevOpsMcp.Domain.Entities.WorkItemRelation;
using ApiWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using ApiWorkItemRelation = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemRelation;

namespace DevOpsMcp.Infrastructure.Repositories;

public sealed class WorkItemRepository(
    IAzureDevOpsClientFactory clientFactory,
    ILogger<WorkItemRepository> logger)
    : IWorkItemRepository
{
    public async Task<DomainWorkItem?> GetByIdAsync(string projectId, int workItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var workItem = await client.GetWorkItemAsync(projectId, workItemId, expand: WorkItemExpand.All, cancellationToken: cancellationToken);
            
            return MapToEntity(workItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting work item {WorkItemId} in project {ProjectId}", workItemId, projectId);
            return null;
        }
    }

    public async Task<IReadOnlyList<DomainWorkItem>> GetByIdsAsync(string projectId, IEnumerable<int> workItemIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var workItems = await client.GetWorkItemsAsync(projectId, workItemIds.ToList(), expand: WorkItemExpand.All, cancellationToken: cancellationToken);
            
            return workItems.Select(MapToEntity).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting work items in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainWorkItem> CreateAsync(string projectId, DomainWorkItem workItem, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var document = new JsonPatchDocument();

            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = "/fields/System.Title",
                Value = workItem.Title
            });

            if (!string.IsNullOrEmpty(workItem.Description))
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Description",
                    Value = workItem.Description
                });
            }

            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = "/fields/System.AreaPath",
                Value = workItem.AreaPath
            });

            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = "/fields/System.IterationPath",
                Value = workItem.IterationPath
            });

            if (!string.IsNullOrEmpty(workItem.AssignedTo))
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.AssignedTo",
                    Value = workItem.AssignedTo
                });
            }

            if (workItem.Priority.HasValue)
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Priority",
                    Value = workItem.Priority.Value
                });
            }

            if (!string.IsNullOrEmpty(workItem.Severity))
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/Microsoft.VSTS.Common.Severity",
                    Value = workItem.Severity
                });
            }

            if (workItem.Tags.Any())
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Tags",
                    Value = string.Join(";", workItem.Tags)
                });
            }

            foreach (var field in workItem.Fields)
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{field.Key}",
                    Value = field.Value
                });
            }

            var createdWorkItem = await client.CreateWorkItemAsync(document, projectId, workItem.WorkItemType, cancellationToken: cancellationToken);
            
            return MapToEntity(createdWorkItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating work item in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainWorkItem> UpdateAsync(string projectId, DomainWorkItem workItem, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var document = new JsonPatchDocument();

            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Replace,
                Path = "/fields/System.Title",
                Value = workItem.Title
            });

            if (!string.IsNullOrEmpty(workItem.Description))
            {
                document.Add(new JsonPatchOperation
                {
                    Operation = Operation.Replace,
                    Path = "/fields/System.Description",
                    Value = workItem.Description
                });
            }

            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Replace,
                Path = "/fields/System.State",
                Value = workItem.State.ToString()
            });

            var updatedWorkItem = await client.UpdateWorkItemAsync(document, workItem.Id, cancellationToken: cancellationToken);
            
            return MapToEntity(updatedWorkItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating work item {WorkItemId} in project {ProjectId}", workItem.Id, projectId);
            throw;
        }
    }

    public async Task DeleteAsync(string projectId, int workItemId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            await client.DeleteWorkItemAsync(workItemId, destroy: true, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting work item {WorkItemId} in project {ProjectId}", workItemId, projectId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DomainWorkItem>> QueryAsync(string projectId, string wiql, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var query = new Wiql { Query = wiql };
            
            var result = await client.QueryByWiqlAsync(query, projectId, cancellationToken: cancellationToken);
            
            if (!result.WorkItems.Any())
            {
                return Array.Empty<DomainWorkItem>();
            }

            var ids = result.WorkItems.Select(wi => wi.Id).ToList();
            var workItems = await client.GetWorkItemsAsync(projectId, ids, expand: WorkItemExpand.All, cancellationToken: cancellationToken);
            
            return workItems.Select(MapToEntity).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error querying work items in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainWorkItem> AddRelationAsync(string projectId, int workItemId, DomainWorkItemRelation relation, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var document = new JsonPatchDocument();

            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = "/relations/-",
                Value = new
                {
                    rel = relation.RelationType,
                    url = relation.TargetUrl,
                    attributes = relation.Attributes
                }
            });

            var updatedWorkItem = await client.UpdateWorkItemAsync(document, workItemId, cancellationToken: cancellationToken);
            
            return MapToEntity(updatedWorkItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding relation to work item {WorkItemId}", workItemId);
            throw;
        }
    }

    public async Task<DomainWorkItem> RemoveRelationAsync(string projectId, int workItemId, string relationUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateWorkItemClient();
            var workItem = await client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.Relations, cancellationToken: cancellationToken);
            
            var relationIndex = workItem.Relations?.ToList().FindIndex(r => r.Url == relationUrl) ?? -1;
            if (relationIndex < 0)
            {
                throw new System.InvalidOperationException($"Relation {relationUrl} not found");
            }

            var document = new JsonPatchDocument();
            document.Add(new JsonPatchOperation
            {
                Operation = Operation.Remove,
                Path = $"/relations/{relationIndex}"
            });

            var updatedWorkItem = await client.UpdateWorkItemAsync(document, workItemId, cancellationToken: cancellationToken);
            
            return MapToEntity(updatedWorkItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing relation from work item {WorkItemId}", workItemId);
            throw;
        }
    }

    private static DomainWorkItem MapToEntity(ApiWorkItem azureWorkItem)
    {
        var fields = azureWorkItem.Fields;
        
        return new DomainWorkItem
        {
            Id = azureWorkItem.Id ?? 0,
            WorkItemType = GetFieldValue<string>(fields, "System.WorkItemType") ?? "Unknown",
            Title = GetFieldValue<string>(fields, "System.Title") ?? string.Empty,
            Description = GetFieldValue<string>(fields, "System.Description"),
            State = ParseWorkItemState(GetFieldValue<string>(fields, "System.State")),
            AssignedTo = GetFieldValue<string>(fields, "System.AssignedTo"),
            AreaPath = GetFieldValue<string>(fields, "System.AreaPath") ?? string.Empty,
            IterationPath = GetFieldValue<string>(fields, "System.IterationPath") ?? string.Empty,
            CreatedDate = GetFieldValue<DateTime>(fields, "System.CreatedDate"),
            CreatedBy = GetFieldValue<string>(fields, "System.CreatedBy") ?? "Unknown",
            ChangedDate = GetFieldValue<DateTime?>(fields, "System.ChangedDate"),
            ChangedBy = GetFieldValue<string>(fields, "System.ChangedBy"),
            Priority = GetFieldValue<int?>(fields, "Microsoft.VSTS.Common.Priority"),
            Severity = GetFieldValue<string>(fields, "Microsoft.VSTS.Common.Severity"),
            Tags = ParseTags(GetFieldValue<string>(fields, "System.Tags")),
            Relations = MapRelations(azureWorkItem.Relations),
            Fields = new Dictionary<string, object>(fields)
        };
    }

    private static T? GetFieldValue<T>(IDictionary<string, object?> fields, string fieldName)
    {
        if (fields.TryGetValue(fieldName, out var value) && value != null)
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
            
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        
        return default;
    }

    private static WorkItemState ParseWorkItemState(string? state)
    {
        return state?.ToLowerInvariant() switch
        {
            "new" => WorkItemState.New,
            "active" => WorkItemState.Active,
            "resolved" => WorkItemState.Resolved,
            "closed" => WorkItemState.Closed,
            "removed" => WorkItemState.Removed,
            _ => WorkItemState.New
        };
    }

    private static List<string> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return new List<string>();
        }
        
        return tags.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static List<DomainWorkItemRelation> MapRelations(IList<Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItemRelation>? relations)
    {
        if (relations == null || !relations.Any())
        {
            return new List<DomainWorkItemRelation>();
        }

        return relations.Select(r => new DomainWorkItemRelation
        {
            RelationType = r.Rel ?? string.Empty,
            TargetUrl = r.Url ?? string.Empty,
            TargetId = ExtractWorkItemId(r.Url),
            Attributes = r.Attributes != null ? new Dictionary<string, object>(r.Attributes) : new Dictionary<string, object>()
        }).ToList();
    }

    private static int ExtractWorkItemId(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return 0;
        }

        var parts = url.Split('/');
        if (parts.Length > 0 && int.TryParse(parts[^1], out var id))
        {
            return id;
        }

        return 0;
    }
}