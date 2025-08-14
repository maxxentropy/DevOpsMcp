using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevOpsMcp.Infrastructure.Repositories.Enhanced;

public class EnhancedTaskRepository : IEnhancedTaskRepository
{
    private readonly EnhancedFeaturesDbContext _context;
    
    public EnhancedTaskRepository(EnhancedFeaturesDbContext context)
    {
        _context = context;
    }
    
    public async Task<DevOpsTask> CreateAsync(DevOpsTask task)
    {
        task.Id = Guid.NewGuid();
        task.CreatedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        
        return task;
    }
    
    public async Task<DevOpsTask?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
    
    public async Task<DevOpsTask> UpdateAsync(DevOpsTask task)
    {
        task.UpdatedAt = DateTime.UtcNow;
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
        
        return task;
    }
    
    public async Task<bool> ArchiveAsync(Guid id, string archivedBy = "system")
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null || task.Archived)
            return false;
            
        task.Archived = true;
        task.ArchivedAt = DateTime.UtcNow;
        task.ArchivedBy = archivedBy;
        task.UpdatedAt = DateTime.UtcNow;
        
        // Archive subtasks
        var subtasks = await _context.Tasks
            .Where(t => t.ParentTaskId == id && !t.Archived)
            .ToListAsync();
            
        foreach (var subtask in subtasks)
        {
            subtask.Archived = true;
            subtask.ArchivedAt = DateTime.UtcNow;
            subtask.ArchivedBy = archivedBy;
            subtask.UpdatedAt = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<List<DevOpsTask>> ListAsync(TaskFilter filter)
    {
        var query = _context.Tasks.AsQueryable();
        
        if (!filter.IncludeDone)
            query = query.Where(t => !t.Archived);
            
        if (filter.ProjectId != null)
            query = query.Where(t => t.ProjectId.ToString() == filter.ProjectId);
            
        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
            
        if (!string.IsNullOrEmpty(filter.Assignee))
            query = query.Where(t => t.Assignee == filter.Assignee);
            
        if (!string.IsNullOrEmpty(filter.Feature))
            query = query.Where(t => t.Feature == filter.Feature);
            
        // Apply sorting
        query = filter.SortBy switch
        {
            TaskSortBy.Priority => filter.SortDescending 
                ? query.OrderByDescending(t => t.TaskOrder)
                : query.OrderBy(t => t.TaskOrder),
            TaskSortBy.CreatedAt => filter.SortDescending
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            TaskSortBy.UpdatedAt => filter.SortDescending
                ? query.OrderByDescending(t => t.UpdatedAt)
                : query.OrderBy(t => t.UpdatedAt),
            TaskSortBy.Status => filter.SortDescending
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            _ => query.OrderByDescending(t => t.TaskOrder)
        };
        
        return await query
            .Skip(filter.Skip)
            .Take(filter.Take)
            .Include(t => t.Project)
            .ToListAsync();
    }
    
    public async Task<int> CountAsync(TaskFilter filter)
    {
        var query = _context.Tasks.AsQueryable();
        
        if (!filter.IncludeDone)
            query = query.Where(t => !t.Archived);
            
        if (filter.ProjectId != null)
            query = query.Where(t => t.ProjectId.ToString() == filter.ProjectId);
            
        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
            
        if (!string.IsNullOrEmpty(filter.Assignee))
            query = query.Where(t => t.Assignee == filter.Assignee);
            
        if (!string.IsNullOrEmpty(filter.Feature))
            query = query.Where(t => t.Feature == filter.Feature);
            
        return await query.CountAsync();
    }
    
    public async Task<List<DevOpsTask>> GetByProjectAsync(Guid projectId, bool includeArchived = false)
    {
        var query = _context.Tasks.Where(t => t.ProjectId == projectId);
        
        if (!includeArchived)
            query = query.Where(t => !t.Archived);
            
        return await query
            .OrderBy(t => t.TaskOrder)
            .Include(t => t.SubTasks)
            .ToListAsync();
    }
    
    public async Task<List<DevOpsTask>> GetByStatusAsync(DevOpsTaskStatus status, Guid? projectId = null)
    {
        var query = _context.Tasks.Where(t => t.Status == status && !t.Archived);
        
        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
            
        return await query
            .OrderBy(t => t.TaskOrder)
            .Include(t => t.Project)
            .ToListAsync();
    }
}