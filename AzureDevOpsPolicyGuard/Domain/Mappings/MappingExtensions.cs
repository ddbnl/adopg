using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Domain.Policies;
using AzureDevOpsPolicyGuard.DTO;
using AzureDevOpsPolicyGuard.Support;


namespace AzureDevOpsPolicyGuard.Domain.Mappings;


public static class PolicyDtoExtensions
{
    public static PolicyDto ToDto(this BasePolicy policy, IOrganizationCacheService organizationCacheService)
    {
        IEnumerable<ViolationDto> violations = policy.Violations.Select(c => new ViolationDto
        {
            Id = c.Id,
            Description = c.Describe()
        }).AsEnumerable();
        return new PolicyDto
        {
            Id = policy.Id,
            Description = policy.GetDescription(),
            Errors = violations,
            Compliant = policy.GetCompliant(),
            LastChecked = policy.GetLastChecked(),
            Pipeline = policy.GetPipelineName(organizationCacheService)
        };
    }
}