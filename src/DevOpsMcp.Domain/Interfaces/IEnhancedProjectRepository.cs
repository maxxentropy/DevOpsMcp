using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;

namespace DevOpsMcp.Domain.Interfaces;

public interface IEnhancedProjectRepository
{
    Task<EnhancedProject> CreateAsync(EnhancedProject project);
    Task<EnhancedProject?> GetByIdAsync(Guid id);
    Task<EnhancedProject> UpdateAsync(EnhancedProject project);
    Task<bool> DeleteAsync(Guid id);
    Task<List<EnhancedProject>> ListAsync(bool includePinned = true);
    Task<List<EnhancedProject>> SearchAsync(string query);
}