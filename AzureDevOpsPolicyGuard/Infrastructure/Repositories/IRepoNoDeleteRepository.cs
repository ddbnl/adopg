using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Domain.Policies;
using AzureDevOpsPolicyGuard.Support;


namespace AzureDevOpsPolicyGuard.Infrastructure.Repositories;


public interface IRepoNoDeleteRepository
{
    public IEnumerable<RepoNoDeletePolicy> GetPolicies();
    public RepoNoDeletePolicy GetById(Guid id);
    public void PopulatePolicies(IOrganizationCacheService organizationCacheService);
    public void UpdatePolicies();
    public RepoNoDeletePolicy? GetPolicy(ProjectCache project, int pipelineId);
}