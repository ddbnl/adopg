using AzureDevOpsPolicyGuard.Application.Common.Enums;
using AzureDevOpsPolicyGuard.Application.Services;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;

namespace AzureDevOpsPolicyGuard.Support;

public class OrganizationCacheService : IOrganizationCacheService
{
    public readonly List<ProjectCache> Projects = [];
    public bool Valid = false;

    public List<ProjectCache> GetProjects()
    {
        return Projects;
    }

    public RepositoryCache? GetGitRepositoryByPipelineId(string project, int pipelineId)
    {
        var projectCache = Projects.First(c => c.Project.Name == project);
        var pipelineCache = projectCache.Pipelines.First(c => c.Pipeline.Id == pipelineId);
        var repoId = pipelineCache.GitRepositoryId();
        if (repoId == null) return null;
        var repository = projectCache.Repos.First(c => c.Repository.Id == repoId);
        return repository;
    }
    public async Task AddProject(IAzureDevopsService azureDevopsService, TeamProjectReference project)
    {
        var projectCache = new ProjectCache(project);
        await projectCache.Regenerate(azureDevopsService);
        Projects.Add(projectCache);
    }
    
    public async Task Regenerate(IAzureDevopsService azureDevopsService)
    {
        Projects.Clear();
        var projects = await azureDevopsService.GetProjects();
        foreach (var project in projects)
        {
            await AddProject(azureDevopsService, project);
        }
        Valid = true;
    }
}

public class ProjectCache : IProjectCache
{
    public readonly TeamProjectReference Project;
    public string ScopeDescriptor;
    public readonly List<RepositoryCache> Repos = [];
    public readonly List<MemberCache> Members = [];
    public readonly List<GroupCache> Groups = [];
    public readonly List<ServicePrincipalCache> ServicePrincipals = [];
    public readonly List<PipelineCache> Pipelines = [];
    public bool Valid = false;

    public ProjectCache(TeamProjectReference project)
    {
        Project = project;
    }

    public bool IsMemberOf(string groupName, Guid id)
    {
        Console.WriteLine("A");
        var groupIdentifier = Groups
            .First(c => c.Group.DisplayName == groupName)
            .Identity
            .Descriptor
            .Identifier;
        var user = Members
            .First(c => c.Identity.Id == id);
        var isMemberOf = user.Memberships.Any(c => c.Descriptor.Identifier == groupIdentifier);
        return isMemberOf;
    }
    
    private async Task AddRepo(IAzureDevopsService azureDevopsService, GitRepository gitRepository)
    {
        var repositoryCache = new RepositoryCache(this, gitRepository);
        await repositoryCache.Regenerate(azureDevopsService);
        Repos.Add(repositoryCache);
    }
    
    private async Task AddPipeline(Pipeline pipeline)
    {
        var pipelineCache = new PipelineCache(Project, pipeline);
        await pipelineCache.Regenerate();
        Pipelines.Add(pipelineCache);
    }
    
    private async Task AddMember(IAzureDevopsService azureDevopsService, GraphMember member)
    {
        var identity = await azureDevopsService.FindIdentity(member.DisplayName);
        var memberships = await azureDevopsService.GetMemberships(member.Descriptor, 
            Groups.Select(c => c.Group).ToList());
        var memberCache = new MemberCache(Project, identity, member, memberships.ToList());
        Members.Add(memberCache);
    }
    private async Task AddGroup(IAzureDevopsService azureDevopsService, GraphGroup group)
    {
        var identity = await azureDevopsService.FindIdentity(group.DisplayName);
        var groupCache = new GroupCache(Project, identity, group);
        Groups.Add(groupCache);
    }
    private async Task AddServicePrincipal(IAzureDevopsService azureDevopsService, GraphServicePrincipal servicePrincipal)
    {
        var identity = await azureDevopsService.FindIdentity(servicePrincipal.DisplayName);
        var servicePrincipalCache = new ServicePrincipalCache(Project, identity, servicePrincipal);
        ServicePrincipals.Add(servicePrincipalCache);
    } 

    public async Task Regenerate(IAzureDevopsService azureDevopsService)
    {
        ScopeDescriptor = await azureDevopsService.GetScope(Project.Id);
        var repos = await azureDevopsService.GetRepos(Project.Name);
        var pipelines = await azureDevopsService.GetPipelines(Project.Name);
        var groups = await azureDevopsService.GetGroups(null);
        var members = await azureDevopsService.GetMembers(ScopeDescriptor);
        var servicePrincipals = await azureDevopsService.GetServicePrincipals(ScopeDescriptor);
        
        foreach (var repo in repos)
        {
            await AddRepo(azureDevopsService, repo);
        }       
        foreach (var pipeline in pipelines)
        {
            var pipelineDetails = await azureDevopsService.GetPipeline(Project.Name, pipeline.Id);
            await AddPipeline(pipelineDetails);
        }       
        foreach (var group in groups)
        {
            await AddGroup(azureDevopsService, group);
        }
        foreach (var member in members)
        {
            await AddMember(azureDevopsService, member);
        }
        foreach (var servicePrincipal in servicePrincipals)
        {
            await AddServicePrincipal(azureDevopsService, servicePrincipal);
        }
        Valid = true;
    }
}

public class RepositoryCache(ProjectCache project, GitRepository repository) : IRepositoryCache
{
    public readonly ProjectCache Project = project;
    public readonly GitRepository Repository = repository;
    public readonly List<RepositoryAclCache> Acls = [];
    public bool Valid = false;

    private void AddAcl(Identity identity, RepoAcl acl, string token)
    {
        var repositoryAcl = new RepositoryAclCache(Project, Repository, identity, acl, token);
        Acls.Add(repositoryAcl);
    }

    public async Task Regenerate(IAzureDevopsService azureDevopsService)
    {
        IEnumerable<string> tokens =
        [
            $"repoV2/{Repository.Id}",
            $"repoV2/{project.Project.Id}",
        ];
        foreach (var token in tokens)
        {
            var acls = await azureDevopsService.GetRepoAcl(token);
            foreach (var acl in acls)
            {
                AddAcl(acl.Key, acl.Value, token);
            }            
        }

        Valid = true;
    }
}

public class PipelineCache(TeamProjectReference project, Pipeline pipeline) : IPipelineCache
{
    public readonly TeamProjectReference Project = project;
    public readonly Pipeline Pipeline = pipeline;
    //public readonly List<PipelineAclCache> Acls = [];
    public bool Valid = false;

    public YamlConfiguration? YamlConfiguration()
    {
        return (Pipeline.Configuration.Type == ConfigurationType.Yaml)
            ? (YamlConfiguration)Pipeline.Configuration
            : null;
    }
    
    public Guid? GitRepositoryId()
    {
        var yamlConfig = YamlConfiguration();
        if (yamlConfig == null || yamlConfig.Repository.Type != RepositoryType.AzureReposGit) return null;
        var pipelineRepo = (AzureReposGitRepository)yamlConfig.Repository;
        return pipelineRepo.Id;
        
    }
    // private void AddAcl(Identity identity, RepoAcl acl)
    // {
    //     var repositoryAcl = new RepositoryAclCache(Project, Repository, identity, acl);
    //     Acls.Add(repositoryAcl);
    // }

    public async Task Regenerate()
    {
        //var acls = await AzureDevops.GetRepoAclByName(Repository.Name, Project.Name);
        // foreach (var acl in acls)
        // {
        //     AddAcl(acl.Key, acl.Value);
        // }
        Valid = true;
    }
}

public class RepositoryAclCache(
    ProjectCache project,
    GitRepository repository,
    Identity identity,
    RepoAcl acl,
    string token
    ) : IRepositoryAclCache
{
    public readonly ProjectCache Project = project;
    public readonly GitRepository Repository = repository;
    public readonly Identity Identity = identity;
    public readonly RepoAcl Acl = acl;
    public readonly string Token = token;
    public bool Valid = false;
}

public class MemberCache(
    TeamProjectReference project,
    Identity identity,
    GraphMember member,
    List<GraphGroup> memberships
    ) : IMemberCache
{
    public readonly TeamProjectReference Project = project;
    public readonly Identity Identity = identity;
    public readonly GraphMember Member = member;
    public readonly List<GraphGroup> Memberships = memberships;
}

public class GroupCache(
    TeamProjectReference project,
    Identity identity,
    GraphGroup group) : IGroupCache
{
    public readonly TeamProjectReference Project = project;
    public readonly Identity Identity = identity;
    public readonly GraphGroup Group = group;
}
public class ServicePrincipalCache(
    TeamProjectReference project,
    Identity identity,
    GraphServicePrincipal servicePrincipal) : IServicePrincipalCache
{
    public readonly TeamProjectReference Project = project;
    public readonly Identity Identity = identity;
    public readonly GraphServicePrincipal ServicePrincipal = servicePrincipal;
}