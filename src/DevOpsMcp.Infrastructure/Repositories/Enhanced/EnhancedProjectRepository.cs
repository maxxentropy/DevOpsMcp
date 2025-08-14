using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevOpsMcp.Infrastructure.Repositories.Enhanced;

public class EnhancedProjectRepository : IEnhancedProjectRepository
{
    private readonly EnhancedFeaturesDbContext _context;
    
    public EnhancedProjectRepository(EnhancedFeaturesDbContext context)
    {
        _context = context;
    }
    
    public async Task<EnhancedProject> CreateAsync(EnhancedProject project)
    {
        project.Id = Guid.NewGuid();
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;
        
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        return project;
    }
    
    public async Task<EnhancedProject?> GetByIdAsync(Guid id)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.ProjectSources)
            .Include(p => p.DocumentVersions)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    
    public async Task<EnhancedProject> UpdateAsync(EnhancedProject project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
        
        return project;
    }
    
    public async Task<bool> DeleteAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return false;
            
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<EnhancedProject>> ListAsync(bool includePinned = true)
    {
        var query = _context.Projects.AsQueryable();
        
        if (!includePinned)
            query = query.Where(p => !p.Pinned);
            
        return await query
            .OrderByDescending(p => p.Pinned)
            .ThenByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }
    
    public async Task<List<EnhancedProject>> SearchAsync(string query)
    {
        return await _context.Projects
            .Where(p => p.Title.Contains(query) || p.Description.Contains(query))
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();
    }
}