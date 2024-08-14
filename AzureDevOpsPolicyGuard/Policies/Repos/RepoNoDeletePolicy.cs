using AzureDevOpsPolicyGuard.Enums;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;

namespace AzureDevOpsPolicyGuard.Policies.Repos;

public class RepoNoDeletePolicy(string project, int pipelineId, string repo): BasePolicy(project, pipelineId)
{
    public readonly string Repo = repo;

    public override Task Remediate()
    {
        throw new NotImplementedException();
    }

    public string PipelineName()
    {
        return OrganizationCache
            .Projects
            .First(c => c.Project.Name == project)
            .Pipelines.First(c => c.Pipeline.Id == PipelineId)
            .Pipeline.Name;
    }

    public override string GetDescription()
    {
        return $"No one should be able to delete the {Repo} repository.";
    }
    
    public override void CheckCompliance()
    {
        ComplianceFailures = [];

        OrganizationCache
            .Projects
            .First(project => project.Project.Name == Project)
            .Repos
            .First(repo => repo.Repository.Name == Repo)
            .Acls
            .Where(acl => (acl.Acl & RepoAcl.DeleteOrDisableRepository) != 0)
            .ForEach(errorAcl =>
                ComplianceFailures.Add($"{errorAcl.Identity.DisplayName} should not be able to delete the repository"));
        
        IsChecked = true;
        LastChecked = DateTimeOffset.Now;
        IsCompliant = ComplianceFailures.IsNullOrEmpty();
    }
}