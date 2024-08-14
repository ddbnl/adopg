using AzureDevOpsPolicyGuard.Support;
using Microsoft.TeamFoundation.Core.WebApi;


namespace AzureDevOpsPolicyGuard.Application.Services;


public interface IOrganizationCacheService
{
    public List<ProjectCache> GetProjects();
    public RepositoryCache? GetGitRepositoryByPipelineId(string project, int pipelineId);
    public Task AddProject(IAzureDevopsService azureDevopsService, TeamProjectReference project);
    public abstract Task Regenerate(IAzureDevopsService azureDevopsService);
}


public interface IProjectCache
{
    public bool IsMemberOf(string groupName, Guid id);
    public Task Regenerate(IAzureDevopsService azureDevopsService);
}


public interface IRepositoryCache
{
    public Task Regenerate(IAzureDevopsService azureDevopsService);
}


public interface IPipelineCache;
public interface IRepositoryAclCache;
public interface IMemberCache;
public interface IGroupCache;
public interface IServicePrincipalCache;