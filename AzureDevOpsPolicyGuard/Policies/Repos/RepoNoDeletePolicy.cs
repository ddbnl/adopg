using AzureDevOpsPolicyGuard.Enums;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;

namespace AzureDevOpsPolicyGuard.Policies.Repos;


public record RemediationRecord
{
    public IdentityDescriptor Identity { get; init; }
    public string Token { get; init; }
}


public class RepoNoDeletePolicy(ProjectCache project, int pipelineId, string repo): BasePolicy(project, pipelineId)
{
    public readonly string Repo = repo;
    public List<RemediationRecord> RemediationDetails = [];

    public override async Task Remediate(int compliancyError)
    {
        var remediate = RemediationDetails[compliancyError];
        await AzureDevops
            .DisableRepoAclFlagByName(remediate.Token, remediate.Identity, RepoAcl.DeleteOrDisableRepository);
    }

    public string PipelineName()
    {
        return OrganizationCache
            .Projects
            .First(c => c.Project.Name == Project.Project.Name)
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
        RemediationDetails = [];

        Project
            .Repos
            .First(repo => repo.Repository.Name == Repo)
            .Acls
            .Where(acl => (acl.Acl & RepoAcl.DeleteOrDisableRepository) != 0)
            .ForEach(errorAcl =>
            {
                var remediate = new RemediationRecord
                {
                    Identity = errorAcl.Identity.Descriptor,
                    Token = errorAcl.Token,

                };
                RemediationDetails.Add(remediate);
                ComplianceFailures.Add(
                    $"{errorAcl.Identity.DisplayName} should not be able to delete the repository");
            });
        
        IsChecked = true;
        LastChecked = DateTimeOffset.Now;
        IsCompliant = ComplianceFailures.IsNullOrEmpty();
    }
}