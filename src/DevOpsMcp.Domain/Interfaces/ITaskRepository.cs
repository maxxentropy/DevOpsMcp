using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Interfaces;

public interface ITaskRepository
{
    Task<DevOpsTask> CreateAsync(DevOpsTask task);
    Task<DevOpsTask?> GetByIdAsync(Guid id);
    Task<DevOpsTask> UpdateAsync(DevOpsTask task);
    Task<bool> DeleteAsync(Guid id);
    Task<List<DevOpsTask>> ListAsync(TaskFilter filter);
    Task<int> CountAsync(TaskFilter filter);
    Task<List<DevOpsTask>> GetByLinkedWorkItemAsync(int workItemId, string? projectId = null);
}

public class TaskFilter
{
    public string? ProjectId { get; set; }
    public DevOpsTaskStatus? Status { get; set; }
    public string? Assignee { get; set; }
    public string? Feature { get; set; }
    public List<string>? Tags { get; init; }
    public bool IncludeDone { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; } = 50;
    public TaskSortBy SortBy { get; set; } = TaskSortBy.Priority;
    public bool SortDescending { get; set; } = true;
}

public enum TaskSortBy
{
    Priority,
    CreatedAt,
    UpdatedAt,
    Status
}