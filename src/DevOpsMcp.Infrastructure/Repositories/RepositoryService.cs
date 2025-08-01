using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using DevOpsMcp.Infrastructure.Services;
using DomainRepository = DevOpsMcp.Domain.Entities.Repository;
using DomainGitRef = DevOpsMcp.Domain.Interfaces.GitRef;
using DomainGitCommit = DevOpsMcp.Domain.Interfaces.GitCommit;
using ApiGitRepository = Microsoft.TeamFoundation.SourceControl.WebApi.GitRepository;
using ApiGitRef = Microsoft.TeamFoundation.SourceControl.WebApi.GitRef;
using ApiGitCommit = Microsoft.TeamFoundation.SourceControl.WebApi.GitCommit;

namespace DevOpsMcp.Infrastructure.Repositories;

public sealed class RepositoryService(
    IAzureDevOpsClientFactory clientFactory,
    ILogger<RepositoryService> logger)
    : IRepositoryService
{
    public async Task<DomainRepository?> GetByIdAsync(string projectId, string repositoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            var repo = await client.GetRepositoryAsync(projectId, repositoryId, cancellationToken: cancellationToken);
            
            return MapToEntity(repo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting repository {RepositoryId} in project {ProjectId}", repositoryId, projectId);
            return null;
        }
    }

    public async Task<IReadOnlyList<DomainRepository>> GetRepositoriesAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            var repos = await client.GetRepositoriesAsync(projectId, cancellationToken: cancellationToken);
            
            return repos.Select(r => MapToEntity(r)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting repositories in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainRepository> CreateAsync(string projectId, CreateRepositoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            
            var gitRepoOptions = new GitRepositoryCreateOptions
            {
                Name = request.Name,
                ProjectReference = new TeamProjectReference { Id = Guid.Parse(projectId) }
            };

            if (!string.IsNullOrEmpty(request.ParentRepositoryId))
            {
                gitRepoOptions.ParentRepository = new GitRepositoryRef 
                { 
                    Id = Guid.Parse(request.ParentRepositoryId) 
                };
            }

            var createdRepo = await client.CreateRepositoryAsync(gitRepoOptions, cancellationToken: cancellationToken);
            
            return MapToEntity(createdRepo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating repository in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainRepository> UpdateAsync(string projectId, string repositoryId, UpdateRepositoryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            
            var repo = await client.GetRepositoryAsync(projectId, repositoryId, cancellationToken: cancellationToken);
            
            if (!string.IsNullOrEmpty(request.DefaultBranch))
            {
                repo.DefaultBranch = request.DefaultBranch;
            }

            if (request.IsDisabled.HasValue)
            {
                repo.IsDisabled = request.IsDisabled.Value;
            }

            var updatedRepo = await client.UpdateRepositoryAsync(repo, Guid.Parse(repositoryId), cancellationToken: cancellationToken);
            
            return MapToEntity(updatedRepo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating repository {RepositoryId} in project {ProjectId}", repositoryId, projectId);
            throw;
        }
    }

    public async Task DeleteAsync(string projectId, string repositoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            await client.DeleteRepositoryAsync(Guid.Parse(repositoryId), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting repository {RepositoryId} in project {ProjectId}", repositoryId, projectId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DomainGitRef>> GetBranchesAsync(string projectId, string repositoryId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            var refs = await client.GetRefsAsync(projectId, repositoryId, filter: "heads/", cancellationToken: cancellationToken);
            
            return refs.Select(r => new DomainGitRef
            {
                Name = r.Name,
                ObjectId = r.ObjectId,
                RefType = GitRefType.Branch
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting branches for repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DomainGitCommit>> GetCommitsAsync(string projectId, string repositoryId, string? branch = null, int? top = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateGitClient();
            
            var searchCriteria = new GitQueryCommitsCriteria
            {
                ItemVersion = new GitVersionDescriptor
                {
                    VersionType = GitVersionType.Branch,
                    Version = branch ?? "main"
                },
                Top = top ?? 100
            };

            var commits = await client.GetCommitsAsync(projectId, repositoryId, searchCriteria, cancellationToken: cancellationToken);
            
            return commits.Select(c => new DomainGitCommit
            {
                CommitId = c.CommitId,
                Author = c.Author.Name,
                AuthorDate = c.Author.Date,
                Committer = c.Committer.Name,
                CommitterDate = c.Committer.Date,
                Comment = c.Comment,
                Parents = c.Parents?.Select(p => p.ToString()).ToList() ?? new List<string>()
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting commits for repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    private static DomainRepository MapToEntity(GitRepository gitRepo)
    {
        return new DomainRepository
        {
            Id = gitRepo.Id.ToString(),
            Name = gitRepo.Name,
            DefaultBranch = gitRepo.DefaultBranch ?? "main",
            Size = gitRepo.Size ?? 0,
            RemoteUrl = gitRepo.RemoteUrl ?? string.Empty,
            WebUrl = gitRepo.WebUrl ?? string.Empty,
            ProjectId = gitRepo.ProjectReference?.Id.ToString() ?? string.Empty,
            IsDisabled = gitRepo.IsDisabled.GetValueOrDefault(),
            IsFork = gitRepo.IsFork,
            ParentRepositoryId = gitRepo.ParentRepository?.Id.ToString()
        };
    }
}