using AzureDevOpsPolicyGuard.Application.Services;

namespace AzureDevOpsPolicyGuard.Domain.Policies;

public abstract class BasePolicyViolation
{
    public readonly Guid Id = Guid.NewGuid();

    public abstract Task Remediate(IAzureDevopsService azureDevopsService);
    public abstract string Describe();
}