using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using DevOpsMcp.Infrastructure.Services;
using DomainPullRequest = DevOpsMcp.Domain.Entities.PullRequest;
using DomainPullRequestStatus = DevOpsMcp.Domain.Entities.PullRequestStatus;
using DomainCommentThreadStatus = DevOpsMcp.Domain.Interfaces.CommentThreadStatus;
using ApiPullRequest = Microsoft.TeamFoundation.SourceControl.WebApi.GitPullRequest;
using ApiPullRequestStatus = Microsoft.TeamFoundation.SourceControl.WebApi.PullRequestStatus;
using GitPullRequestStatus = Microsoft.TeamFoundation.SourceControl.WebApi.GitPullRequestStatus;
using ApiCommentThreadStatus = Microsoft.TeamFoundation.SourceControl.WebApi.CommentThreadStatus;

namespace DevOpsMcp.Infrastructure.Repositories;

public sealed class PullRequestService : IPullRequestService
{
    private readonly IAzureDevOpsClientFactory _clientFactory;
    private readonly ILogger<PullRequestService> _logger;

    public PullRequestService(
        IAzureDevOpsClientFactory clientFactory,
        ILogger<PullRequestService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<DomainPullRequest?> GetByIdAsync(string projectId, string repositoryId, int pullRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            return MapToEntity(pr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull request {PullRequestId}", pullRequestId);
            return null;
        }
    }

    public async Task<IReadOnlyList<DomainPullRequest>> GetPullRequestsAsync(string projectId, string repositoryId, PullRequestFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var searchCriteria = new GitPullRequestSearchCriteria
            {
                Status = filter?.Status != null ? MapStatus(filter.Status.Value) : null,
                CreatorId = filter?.CreatorId != null ? Guid.Parse(filter.CreatorId) : null,
                ReviewerId = filter?.ReviewerId != null ? Guid.Parse(filter.ReviewerId) : null,
                SourceRefName = filter?.SourceBranch,
                TargetRefName = filter?.TargetBranch
            };

            var prs = await client.GetPullRequestsAsync(projectId, repositoryId, searchCriteria, top: filter?.Top, cancellationToken: cancellationToken);
            
            return prs.Select(pr => MapToEntity(pr)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pull requests in repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<DomainPullRequest> CreateAsync(string projectId, string repositoryId, CreatePullRequestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var gitPr = new GitPullRequest
            {
                Title = request.Title,
                Description = request.Description,
                SourceRefName = request.SourceRefName,
                TargetRefName = request.TargetRefName,
                IsDraft = request.IsDraft
            };

            if (request.ReviewerIds != null)
            {
                gitPr.Reviewers = request.ReviewerIds.Select(id => new IdentityRefWithVote
                {
                    Id = id
                }).ToArray();
            }

            if (request.WorkItemIds != null)
            {
                gitPr.WorkItemRefs = request.WorkItemIds.Select(id => new ResourceRef
                {
                    Id = id.ToString()
                }).ToArray();
            }

            var createdPr = await client.CreatePullRequestAsync(gitPr, projectId, repositoryId, cancellationToken: cancellationToken);
            
            if (request.Labels != null && request.Labels.Any())
            {
                foreach (var label in request.Labels)
                {
                    await client.CreatePullRequestLabelAsync(
                        new WebApiCreateTagRequestData { Name = label },
                        repositoryId,
                        createdPr.PullRequestId,
                        cancellationToken: cancellationToken);
                }
            }

            return MapToEntity(createdPr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pull request in repository {RepositoryId}", repositoryId);
            throw;
        }
    }

    public async Task<DomainPullRequest> UpdateAsync(string projectId, string repositoryId, int pullRequestId, UpdatePullRequestRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            if (!string.IsNullOrEmpty(request.Title))
            {
                pr.Title = request.Title;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                pr.Description = request.Description;
            }

            if (request.IsDraft.HasValue)
            {
                pr.IsDraft = request.IsDraft.Value;
            }

            if (request.AutoCompleteOptions != null)
            {
                pr.CompletionOptions = MapCompletionOptions(request.AutoCompleteOptions);
            }

            var updatedPr = await client.UpdatePullRequestAsync(pr, projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            return MapToEntity(updatedPr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<PullRequest> CompletePullRequestAsync(string projectId, string repositoryId, int pullRequestId, PullRequestCompletionOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            pr.Status = ApiPullRequestStatus.Completed;
            pr.CompletionOptions = MapCompletionOptions(options);
            pr.LastMergeSourceCommit = pr.LastMergeSourceCommit;

            var completedPr = await client.UpdatePullRequestAsync(pr, projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            return MapToEntity(completedPr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<DomainPullRequest> AbandonPullRequestAsync(string projectId, string repositoryId, int pullRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            pr.Status = ApiPullRequestStatus.Abandoned;

            var abandonedPr = await client.UpdatePullRequestAsync(pr, projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            return MapToEntity(abandonedPr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<DomainPullRequest> ReactivatePullRequestAsync(string projectId, string repositoryId, int pullRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            pr.Status = ApiPullRequestStatus.Active;

            var reactivatedPr = await client.UpdatePullRequestAsync(pr, projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            
            return MapToEntity(reactivatedPr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<DomainPullRequest> AddReviewerAsync(string projectId, string repositoryId, int pullRequestId, string reviewerId, bool isRequired = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var reviewer = new IdentityRefWithVote
            {
                Id = reviewerId,
                IsRequired = isRequired
            };

            await client.CreatePullRequestReviewerAsync(reviewer, projectId, repositoryId, pullRequestId, reviewerId, cancellationToken: cancellationToken);
            
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            return MapToEntity(pr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding reviewer to pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<PullRequest> VoteAsync(string projectId, string repositoryId, int pullRequestId, int vote, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var reviewer = new IdentityRefWithVote
            {
                Vote = (short)vote
            };

            var currentUserId = Guid.NewGuid().ToString(); // TODO: Get from auth context
            await client.CreatePullRequestReviewerAsync(reviewer, repositoryId, pullRequestId, currentUserId, cancellationToken: cancellationToken);
            
            var pr = await client.GetPullRequestAsync(projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            return MapToEntity(pr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    public async Task<PullRequestComment> AddCommentAsync(string projectId, string repositoryId, int pullRequestId, string content, string? parentCommentId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateGitClient();
            
            var thread = new GitPullRequestCommentThread
            {
                Comments = new List<Microsoft.TeamFoundation.SourceControl.WebApi.Comment>
                {
                    new()
                    {
                        Content = content,
                        ParentCommentId = parentCommentId != null ? (short)int.Parse(parentCommentId) : (short)0
                    }
                }
            };

            var createdThread = await client.CreateThreadAsync(thread, projectId, repositoryId, pullRequestId, cancellationToken: cancellationToken);
            var comment = createdThread.Comments.First();
            
            return new PullRequestComment
            {
                Id = 0, // TODO: Fix type - comment.Id.GetValueOrDefault(),
                Content = comment.Content ?? string.Empty,
                Author = comment.Author?.DisplayName ?? "Unknown",
                PublishedDate = DateTime.UtcNow, // TODO: Fix type - comment.PublishedDate.GetValueOrDefault(),
                ParentCommentId = comment.ParentCommentId > 0 ? (int?)comment.ParentCommentId : null,
                ThreadStatus = MapThreadStatus(createdThread.Status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment to pull request {PullRequestId}", pullRequestId);
            throw;
        }
    }

    private static DomainPullRequest MapToEntity(GitPullRequest gitPr)
    {
        return new DomainPullRequest
        {
            Id = gitPr.PullRequestId,
            Title = gitPr.Title ?? string.Empty,
            Description = gitPr.Description ?? string.Empty,
            SourceRefName = gitPr.SourceRefName,
            TargetRefName = gitPr.TargetRefName,
            Status = gitPr.Status == ApiPullRequestStatus.Completed ? DomainPullRequestStatus.Completed : 
                    gitPr.Status == ApiPullRequestStatus.Abandoned ? DomainPullRequestStatus.Abandoned :
                    gitPr.Status == ApiPullRequestStatus.Active ? DomainPullRequestStatus.Active : 
                    DomainPullRequestStatus.NotSet,
            CreatedBy = gitPr.CreatedBy?.DisplayName ?? "Unknown",
            CreationDate = gitPr.CreationDate,
            ClosedDate = gitPr.ClosedDate,
            RepositoryId = gitPr.Repository?.Id.ToString() ?? string.Empty,
            MergeId = gitPr.LastMergeCommit?.CommitId,
            MergeStatus = MapMergeStatus(gitPr.MergeStatus),
            IsDraft = gitPr.IsDraft.GetValueOrDefault(false),
            Reviewers = gitPr.Reviewers?.Select(r => new PullRequestReviewer
            {
                ReviewerId = r.Id ?? string.Empty,
                DisplayName = r.DisplayName ?? "Unknown",
                Vote = r.Vote,
                IsRequired = false, // TODO: Fix type - r.IsRequired.GetValueOrDefault(),
                HasDeclined = r.HasDeclined.GetValueOrDefault(),
                IsFlagged = r.IsFlagged.GetValueOrDefault()
            }).ToList() ?? new List<PullRequestReviewer>(),
            Labels = gitPr.Labels?.Select(l => l.Name).ToList() ?? new List<string>(),
            WorkItemRefs = gitPr.WorkItemRefs?.Select(w => int.Parse(w.Id ?? "0")).ToList() ?? new List<int>(),
            CompletionOptions = gitPr.CompletionOptions != null ? MapCompletionOptions(gitPr.CompletionOptions) : null,
            AutoCompleteSetBy = gitPr.AutoCompleteSetBy?.DisplayName
        };
    }

    // Removed - not needed anymore

    private static ApiPullRequestStatus? MapStatus(DomainPullRequestStatus status)
    {
        return status switch
        {
            DomainPullRequestStatus.Active => ApiPullRequestStatus.Active,
            DomainPullRequestStatus.Abandoned => ApiPullRequestStatus.Abandoned,
            DomainPullRequestStatus.Completed => ApiPullRequestStatus.Completed,
            _ => null
        };
    }

    private static DomainPullRequestStatus? MapStatusToDomain(ApiPullRequestStatus? status)
    {
        return status switch
        {
            ApiPullRequestStatus.Active => DomainPullRequestStatus.Active,
            ApiPullRequestStatus.Abandoned => DomainPullRequestStatus.Abandoned,
            ApiPullRequestStatus.Completed => DomainPullRequestStatus.Completed,
            _ => null
        };
    }

    private static MergeStatus MapMergeStatus(object? mergeStatus) // TODO: Find correct type GitPullRequestMergeStatus
    {
        // TODO: Find correct enum type for GitPullRequestMergeStatus
        return MergeStatus.NotSet;
    }

    private static GitPullRequestCompletionOptions MapCompletionOptions(PullRequestCompletionOptions options)
    {
        return new GitPullRequestCompletionOptions
        {
            DeleteSourceBranch = options.DeleteSourceBranch,
            SquashMerge = options.SquashMerge,
            MergeStrategy = MapMergeStrategy(options.MergeStrategy),
            TransitionWorkItems = options.TransitionWorkItems,
            AutoCompleteIgnoreConfigIds = new List<int>()
        };
    }

    private static PullRequestCompletionOptions MapCompletionOptions(GitPullRequestCompletionOptions options)
    {
        return new PullRequestCompletionOptions
        {
            DeleteSourceBranch = false, // TODO: Fix type - options.DeleteSourceBranch ?? false,
            SquashMerge = false, // TODO: Fix type - options.SquashMerge ?? false,
            MergeStrategy = MapMergeStrategy(options.MergeStrategy),
            TransitionWorkItems = false, // TODO: Fix type - options.TransitionWorkItems ?? false,
            AutoComplete = false
        };
    }

    private static GitPullRequestMergeStrategy MapMergeStrategy(MergeStrategy strategy)
    {
        return strategy switch
        {
            MergeStrategy.NoFastForward => GitPullRequestMergeStrategy.NoFastForward,
            MergeStrategy.Squash => GitPullRequestMergeStrategy.Squash,
            MergeStrategy.Rebase => GitPullRequestMergeStrategy.Rebase,
            MergeStrategy.RebaseMerge => GitPullRequestMergeStrategy.RebaseMerge,
            _ => GitPullRequestMergeStrategy.NoFastForward
        };
    }

    private static MergeStrategy MapMergeStrategy(GitPullRequestMergeStrategy? strategy)
    {
        return strategy switch
        {
            GitPullRequestMergeStrategy.NoFastForward => MergeStrategy.NoFastForward,
            GitPullRequestMergeStrategy.Squash => MergeStrategy.Squash,
            GitPullRequestMergeStrategy.Rebase => MergeStrategy.Rebase,
            GitPullRequestMergeStrategy.RebaseMerge => MergeStrategy.RebaseMerge,
            _ => MergeStrategy.NoFastForward
        };
    }

    private static DomainCommentThreadStatus MapThreadStatus(ApiCommentThreadStatus? status)
    {
        return status switch
        {
            ApiCommentThreadStatus.Active => DomainCommentThreadStatus.Active,
            ApiCommentThreadStatus.Fixed => DomainCommentThreadStatus.Fixed,
            ApiCommentThreadStatus.WontFix => DomainCommentThreadStatus.WontFix,
            ApiCommentThreadStatus.Closed => DomainCommentThreadStatus.Closed,
            ApiCommentThreadStatus.ByDesign => DomainCommentThreadStatus.ByDesign,
            ApiCommentThreadStatus.Pending => DomainCommentThreadStatus.Pending,
            _ => DomainCommentThreadStatus.Unknown
        };
    }
}