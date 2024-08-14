using AzureDevOpsPolicyGuard.Application.Common.Enums;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Security;

namespace AzureDevOpsPolicyGuard.Application.Services;

public interface IAzureDevopsService
{
    public void SetOrganization(string organization);
    public Task<IEnumerable<TeamProjectReference>> GetProjects();
    public Task<string> GetScope(Guid id);
    public Task<IEnumerable<GraphMember>> GetMembers(string? scope);
    public Task<IEnumerable<GraphGroup>> GetMemberships(string user, List<GraphGroup> allGroups);
    public Task<IEnumerable<GraphGroup>> GetGroups(string? scope);
    public Task<IEnumerable<GraphServicePrincipal>> GetServicePrincipals(string scope);
    public Task<List<Pipeline>> GetPipelines(string project);
    public Task<Pipeline> GetPipeline(string project, int pipelineId);
    public Task<IEnumerable<SecurityNamespaceDescription>> GetSecurityNamespaces();
    public Task<SecurityNamespaceDescription> GetSecurityNamespaceByName(string name);
    public Task<IEnumerable<GitRepository>> GetRepos(string project);
    public Task<GitRepository> GetRepoByName(string name, string project);
    public Task<Identity> FindIdentityById(string id);
    public Task<Identity> FindIdentity(string displayName);
    public Task<Dictionary<Identity, RepoAcl>> GetRepoAcl(string token);
    public Task DisableRepoAclFlagByName(string token, IdentityDescriptor userId, RepoAcl toRemove);

}