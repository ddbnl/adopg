using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Domain.Policies;
using AzureDevOpsPolicyGuard.Support;


namespace AzureDevOpsPolicyGuard.Infrastructure.Repositories;


public class RepoNoDeleteRepository : IRepoNoDeleteRepository
{
    public List<RepoNoDeletePolicy> Policies = [];

    public IEnumerable<RepoNoDeletePolicy> GetPolicies()
    {
        return Policies;
    }

    public RepoNoDeletePolicy GetById(Guid id)
    {
        return Policies
            .First(c => c.Id == id);
    }

    public void PopulatePolicies(IOrganizationCacheService organizationCacheService)
    {
        Policies.Clear();
        foreach (var project in organizationCacheService.GetProjects())
        {
            foreach (var pipeline in project.Pipelines)
            {
                var repository =
                    organizationCacheService.GetGitRepositoryByPipelineId(project.Project.Name, pipeline.Pipeline.Id);
                if (repository != null)
                {
                    var policy = new RepoNoDeletePolicy(project, pipeline.Pipeline.Id, repository.Repository.Name);
                    Policies.Add(policy);
                }
            }
        }
    }

    public void UpdatePolicies()
    {
        foreach (var policy in Policies)
        {
            policy.CheckCompliance();
        }
    }

    public RepoNoDeletePolicy? GetPolicy(ProjectCache project, int pipelineId)
    {
        return Policies.Find(policy => policy.Project.Project.Name == project.Project.Name && policy.PipelineId == pipelineId);
    }
}