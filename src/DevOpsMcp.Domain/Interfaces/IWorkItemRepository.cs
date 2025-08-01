using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Interfaces;

public interface IWorkItemRepository
{
    Task<WorkItem?> GetByIdAsync(string projectId, int workItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItem>> GetByIdsAsync(string projectId, IEnumerable<int> workItemIds, CancellationToken cancellationToken = default);
    Task<WorkItem> CreateAsync(string projectId, WorkItem workItem, CancellationToken cancellationToken = default);
    Task<WorkItem> UpdateAsync(string projectId, WorkItem workItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(string projectId, int workItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItem>> QueryAsync(string projectId, string wiql, CancellationToken cancellationToken = default);
    Task<WorkItem> AddRelationAsync(string projectId, int workItemId, WorkItemRelation relation, CancellationToken cancellationToken = default);
    Task<WorkItem> RemoveRelationAsync(string projectId, int workItemId, string relationUrl, CancellationToken cancellationToken = default);
}