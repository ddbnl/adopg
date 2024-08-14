using AzureDevOpsPolicyGuard.Application.Common.Enums;
using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.VisualStudio.Services.Identity;

namespace AzureDevOpsPolicyGuard.Domain.Policies;

public class RepoNoDeleteViolation : BasePolicyViolation
{
    
    public Identity Identity { get; init; }
    public string Token { get; init; }
    
    public override async Task Remediate(IAzureDevopsService azureDevopsService)
    {
        await azureDevopsService
            .DisableRepoAclFlagByName(Token, Identity.Descriptor, RepoAcl.DeleteOrDisableRepository);
    }

    public override string Describe()
    {
        return $"{Identity.DisplayName} Should not be able to delete this repository.";
    }
}