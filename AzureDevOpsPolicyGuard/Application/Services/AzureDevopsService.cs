using Azure.Identity;
using AzureDevOpsPolicyGuard.Application.Common.Enums;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Security;
using Microsoft.VisualStudio.Services.Security.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOpsPolicyGuard.Application.Services;

public class AzureDevopsService : IAzureDevopsService
{
    private VssConnection? _connection;
    private Uri? _baseUri;

    public void SetOrganization(string organization)
    {
        _baseUri = new Uri($"https://dev.azure.com/{organization}");
    }

    private VssConnection GetConnection()
    {
        if (_connection == null)
        {
            Connect();
        }
        return _connection!;
    }

    private void Connect()
    {
        if (_baseUri == null)
        {
            throw new Exception("No organization set. Call connect first.");
        }
        var azCredential = new InteractiveBrowserCredential();
        var adoCredential = new VssAzureIdentityCredential(azCredential);
        var settings = VssClientHttpRequestSettings.Default.Clone();
        _connection = new VssConnection(_baseUri, adoCredential, settings);
    }

    public async Task<IEnumerable<TeamProjectReference>> GetProjects()
    {
        var connection = GetConnection();
        var client = connection.GetClient<ProjectHttpClient>();
        TeamProjectReference[] aggregate = [];
        string? continuationToken = null;
        do
        {
            var projects = await client.GetProjects(continuationToken: continuationToken);
            continuationToken = projects.ContinuationToken;
            aggregate = aggregate.Concat(projects).ToArray();
        } while (continuationToken != null);

        return aggregate;
    }

    public async Task<string> GetScope(Guid id)
    {
        var connection = GetConnection();
        var client = connection.GetClient<GraphHttpClient>();
        var descriptor = await client.GetDescriptorAsync(id);
        return descriptor.Value;
    }
    
    public async Task<IEnumerable<GraphMember>> GetMembers(string? scope)
    {
        var connection = GetConnection();
        var client = connection.GetClient<GraphHttpClient>();
        GraphMember[] aggregate = [];
        string? continuationToken = null;
        do
        {
            var users = await client.ListMembersAsync(continuationToken: continuationToken, scopeDescriptor: scope);
            continuationToken = users.ContinuationToken.IsNullOrEmpty() ? null : users.ContinuationToken.First();
            aggregate = aggregate.Concat(users.GraphMembers).ToArray();
        } while (continuationToken != null);
        return aggregate;
    }

    public async Task<IEnumerable<GraphGroup>> GetMemberships(string user, List<GraphGroup> allGroups)
    {
        var connection = GetConnection();
        var client = connection.GetClient<GraphHttpClient>();
        var memberships = await client.ListMembershipsAsync(user);
        var ids = memberships.Select(c => c.ContainerDescriptor.Identifier);
        return allGroups.Where(c => ids.Contains(c.Descriptor.Identifier));
    }
    
    public async Task<IEnumerable<GraphGroup>> GetGroups(string? scope)
    { 
        var connection = GetConnection();
        var client = connection.GetClient<GraphHttpClient>();
        GraphGroup[] aggregate = [];
        string? continuationToken = null;
        do
        {
            var groups = await client.ListGroupsAsync(continuationToken: continuationToken, scopeDescriptor: scope);
            continuationToken = groups.ContinuationToken.IsNullOrEmpty() ? null : groups.ContinuationToken.First();
            aggregate = aggregate.Concat(groups.GraphGroups).ToArray();
        } while (continuationToken != null);
 
        return aggregate;
    }     
    
    public async Task<IEnumerable<GraphServicePrincipal>> GetServicePrincipals(string scope)
    {
        var connection = GetConnection();
        var client = connection.GetClient<GraphHttpClient>();
        GraphServicePrincipal[] aggregate = [];
        string? continuationToken = null;
        do
        {
            var servicePrincipals = await client.ListServicePrincipalsAsync(continuationToken: continuationToken, scopeDescriptor: scope);
            continuationToken = servicePrincipals.ContinuationToken.IsNullOrEmpty() ? null : servicePrincipals.ContinuationToken.First();
            aggregate = aggregate.Concat(servicePrincipals.GraphServicePrincipals).ToArray();
        } while (continuationToken != null);

        return aggregate;
    }    
    public async Task<List<Pipeline>> GetPipelines(string project)
    {
        var connection = GetConnection();
        var client = connection.GetClient<PipelinesHttpClient>();
        var pipelines = await client.ListPipelinesAsync(project);
        return pipelines; 
    }

    public async Task<Pipeline> GetPipeline(string project, int pipelineId)
    {
        var connection = GetConnection();
        var client = connection.GetClient<PipelinesHttpClient>();
        var pipeline = await client.GetPipelineAsync(project, pipelineId);
        return pipeline; 
    }   
    
    public async Task<IEnumerable<SecurityNamespaceDescription>> GetSecurityNamespaces()
    {
        var connection = GetConnection();
        var client = connection.GetClient<SecurityHttpClient>();
        return await client.QuerySecurityNamespacesAsync(Guid.Empty);
    }

    public async Task<SecurityNamespaceDescription> GetSecurityNamespaceByName(string name)
    {
        var namespaces = await GetSecurityNamespaces();
        return namespaces
            .First(c => c.Name == name);
    }
    
    public async Task<IEnumerable<GitRepository>> GetRepos(string project)
    {
        var connection = GetConnection();
        var gitClient = connection.GetClient<GitHttpClient>();
        var repos = await gitClient.GetRepositoriesAsync(project);
        return repos;
    }    

    public async Task<GitRepository> GetRepoByName(string name, string project)
    {
        var repos = await GetRepos(project);
        return repos
            .First(c => c.Name == name);
    }

    public async Task<Identity> FindIdentityById(string id)
    {
        var connection = GetConnection();
        var client = connection.GetClient<IdentityHttpClient>();
        var resolvedIdentity = await client.ReadIdentitiesAsync(IdentitySearchFilter.Identifier, id);
        return resolvedIdentity.First();
    }

    public async Task<Identity> FindIdentity(string displayName)
    {
        var connection = GetConnection();
        var client = connection.GetClient<IdentityHttpClient>();
        var identities = await client.ReadIdentitiesAsync(IdentitySearchFilter.DisplayName, displayName);
        var identity = identities.First();
        return identity;
    }
    
    public async Task<Dictionary<Identity, RepoAcl>> GetRepoAcl(string token)
    {
        var result = new Dictionary<Identity, RepoAcl>();
    
        var connection = GetConnection();
        var client = connection.GetClient<SecurityHttpClient>();
        var gitRepoNamespace = await GetSecurityNamespaceByName("Git Repositories");
        var acls = await client.QueryAccessControlListsAsync(
            securityNamespaceId: gitRepoNamespace.NamespaceId,
            token: token,
            descriptors: [],
            includeExtendedInfo: true,
            recurse: false
            );

        foreach (var acl in acls)
        {
            foreach (var sid in acl.AcesDictionary.Keys)
            {
                var permissions = (RepoAcl)acl.AcesDictionary[sid].Allow;
                var identity = await FindIdentityById(sid.Identifier);
                if (!result.ContainsKey(identity))
                {
                    result.Add(identity, permissions);
                }
            }
        }
        
        return result;
    }

    public async Task DisableRepoAclFlagByName(
        string token,
        IdentityDescriptor userId,
        RepoAcl toRemove
        )
    {
        var connection = GetConnection();
        var client = connection.GetClient<SecurityHttpClient>();
        var gitRepoNamespace = await GetSecurityNamespaceByName("Git Repositories");
        await client.RemovePermissionAsync(
            securityNamespaceId: gitRepoNamespace.NamespaceId,
            token: token,
            descriptor: userId,
            permissions: (int)toRemove
        );
    }
}