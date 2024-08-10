using AzureDevOpsPolicyGuard.Policies.Repos;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace AzureDevOpsPolicyGuard.Policies;

public static class RepoNoDeleteRepository
{
    public static List<RepoNoDeletePolicy> Policies = [];

    public static RepoNoDeletePolicy GetById(Guid id)
    {
        return Policies
            .First(c => c.Id == id);
    }

    public static void PopulatePolicies()
    {
        foreach (var project in OrganizationCache.Projects)
        {
            foreach (var pipeline in project.Pipelines)
            {
                var repository =
                    OrganizationCache.GetGitRepositoryByPipelineId(project.Project.Name, pipeline.Pipeline.Id);
                if (repository != null)
                {
                    var policy = new RepoNoDeletePolicy(project, pipeline.Pipeline.Id, repository.Repository.Name);
                    Policies.Add(policy);
                }
            }
        }
    }

    public static void UpdatePolicies()
    {
        foreach (var policy in Policies)
        {
            policy.CheckCompliance();
        }
    }

    public static RepoNoDeletePolicy? GetPolicy(ProjectCache project, int pipelineId)
    {
        return Policies.Find(policy => policy.Project.Project.Name == project.Project.Name && policy.PipelineId == pipelineId);
    }
}