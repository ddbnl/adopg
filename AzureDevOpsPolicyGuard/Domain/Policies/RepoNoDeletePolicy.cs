using AzureDevOpsPolicyGuard.Application.Common.Enums;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.VisualStudio.Services.Common;

namespace AzureDevOpsPolicyGuard.Domain.Policies;


public class RepoNoDeletePolicy(ProjectCache project, int pipelineId, string repo): BasePolicy(project, pipelineId)
{
    public readonly string Repo = repo;

    public override string GetDescription()
    {
        return $"No one should be able to delete the {Repo} repository.";
    }
    
    public override void CheckCompliance()
    {
        
        Violations.Clear();
        Project
            .Repos
            .First(repo => repo.Repository.Name == Repo)
            .Acls
            .Where(acl => (acl.Acl & RepoAcl.DeleteOrDisableRepository) != 0)
            .ForEach(errorAcl =>
            {
                var violation = new RepoNoDeleteViolation
                {
                    Identity = errorAcl.Identity,
                    Token = errorAcl.Token,
                };
                Violations.Add(violation);
            });
        
        IsChecked = true;
        LastChecked = DateTimeOffset.Now;
    }
}