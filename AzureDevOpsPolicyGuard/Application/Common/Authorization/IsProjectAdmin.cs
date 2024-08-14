using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Support;

namespace AzureDevOpsPolicyGuard.Application.Common.Authorization;

public static class IsProjectAdmin
{
    public static bool Evaluate(string projectName, string groupName, Guid userId, IOrganizationCacheService organizationCacheService)
    {
        return organizationCacheService
            .GetProjects()
            .First(c => c.Project.Name == projectName)
            .IsMemberOf(groupName, userId);
    }
}