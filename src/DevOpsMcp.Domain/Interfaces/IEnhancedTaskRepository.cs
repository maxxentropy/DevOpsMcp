using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Interfaces;

public interface IEnhancedTaskRepository
{
    Task<DevOpsTask> CreateAsync(DevOpsTask task);
    Task<DevOpsTask?> GetByIdAsync(Guid id);
    Task<DevOpsTask> UpdateAsync(DevOpsTask task);
    Task<bool> ArchiveAsync(Guid id, string archivedBy = "system");
    Task<List<DevOpsTask>> ListAsync(TaskFilter filter);
    Task<int> CountAsync(TaskFilter filter);
    Task<List<DevOpsTask>> GetByProjectAsync(Guid projectId, bool includeArchived = false);
    Task<List<DevOpsTask>> GetByStatusAsync(DevOpsTaskStatus status, Guid? projectId = null);
}