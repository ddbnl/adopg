namespace AzureDevOpsPolicyGuard.Enums;

[Flags]
public enum RepoAcl
{
    Administer = 1,
    Read = 2,
    Contribute = 4,
    ForcePush = 8,
    CreateBranch = 16,
    CreateTag = 32,
    ManageNotes = 64,
    BypassPoliciesWhenPushing = 128,
    CreateRepository = 256,
    DeleteOrDisableRepository = 512,
    RenameRepository = 1024,
    EditPolicies = 2048,
    RemoveOthersLocks = 4096,
    ManagePermissions = 8192,
    ContributeToPullRequests = 16384,
    BypassPoliciesWhenCompletingPullRequests = 32768,
    AdvancedSecurityViewAlerts = 65536,
    AdvancedSecurityManageAndDismissAlerts = 131072,
    AdvancedSecurityManageSettings = 262144,
}

public static class RepoAclExtensions
{
    public static RepoAcl DisableFlag(this RepoAcl acl, RepoAcl flag)
    {
        if (acl.HasFlag(flag))
        {
            var newValue = (int)acl - (int)flag;
            return (RepoAcl)newValue;
        }
        else
        {
            return acl;
        }
    }
    
}