using AzureDevOpsPolicyGuard.Support;

namespace DefaultNamespace;

public static class IsProjectAdmin
{
    public static bool Evaluate(string projectName, string groupName, Guid userId)
    {
        Console.WriteLine("A");
        return OrganizationCache
            .Projects
            .First(c => c.Project.Name == projectName)
            .IsMemberOf(groupName, userId);
    }
}