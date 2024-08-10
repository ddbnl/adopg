using AzureDevOpsPolicyGuard.Enums;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Graph;
using Microsoft.VisualStudio.Services.Graph.Client;
using Microsoft.VisualStudio.Services.Identity;

namespace AzureDevOpsPolicyGuard.Support;

public static class OrganizationCache
{
    public static readonly List<ProjectCache> Projects = [];
    public static bool Valid = false;

    public static RepositoryCache? GetGitRepositoryByPipelineId(string project, int pipelineId)
    {
        var projectCache = Projects.First(c => c.Project.Name == project);
        var pipelineCache = projectCache.Pipelines.First(c => c.Pipeline.Id == pipelineId);
        var repoId = pipelineCache.GitRepositoryId();
        if (repoId == null) return null;
        var repository = projectCache.Repos.First(c => c.Repository.Id == repoId);
        return repository;
    }
    public static async Task AddProject(TeamProjectReference project)
    {
        var projectCache = new ProjectCache(project);
        await projectCache.Regenerate();
        Projects.Add(projectCache);
    }
    
    public static async Task Regenerate()
    {
        Projects.Clear();
        var projects = await AzureDevops.GetProjects();
        foreach (var project in projects)
        {
            AzureDevops.GetPipelines(project.Name);
            await AddProject(project);
        }
        Valid = true;
    }
}

public class ProjectCache
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
    
    private async Task AddRepo(GitRepository gitRepository)
    {
        var repositoryCache = new RepositoryCache(this, gitRepository);
        await repositoryCache.Regenerate();
        Repos.Add(repositoryCache);
    }
    
    private async Task AddPipeline(Pipeline pipeline)
    {
        var pipelineCache = new PipelineCache(Project, pipeline);
        await pipelineCache.Regenerate();
        Pipelines.Add(pipelineCache);
    }
    
    private async Task AddMember(GraphMember member)
    {
        var identity = await AzureDevops.FindIdentity(member.DisplayName);
        var memberships = await AzureDevops.GetMemberships(member.Descriptor, 
            Groups.Select(c => c.Group).ToList());
        var memberCache = new MemberCache(Project, identity, member, memberships.ToList());
        Members.Add(memberCache);
    }
    private async Task AddGroup(GraphGroup group)
    {
        var identity = await AzureDevops.FindIdentity(group.DisplayName);
        var groupCache = new GroupCache(Project, identity, group);
        Groups.Add(groupCache);
    }
    private async Task AddServicePrincipal(GraphServicePrincipal servicePrincipal)
    {
        var identity = await AzureDevops.FindIdentity(servicePrincipal.DisplayName);
        var servicePrincipalCache = new ServicePrincipalCache(Project, identity, servicePrincipal);
        ServicePrincipals.Add(servicePrincipalCache);
    } 

    public async Task Regenerate()
    {
        ScopeDescriptor = await AzureDevops.GetScope(Project.Id);
        var repos = await AzureDevops.GetRepos(Project.Name);
        var pipelines = await AzureDevops.GetPipelines(Project.Name);
        var groups = await AzureDevops.GetGroups(null);
        var members = await AzureDevops.GetMembers(ScopeDescriptor);
        var servicePrincipals = await AzureDevops.GetServicePrincipals(ScopeDescriptor);
        
        foreach (var repo in repos)
        {
            await AddRepo(repo);
        }       
        foreach (var pipeline in pipelines)
        {
            var pipelineDetails = await AzureDevops.GetPipeline(Project.Name, pipeline.Id);
            await AddPipeline(pipelineDetails);
        }       
        foreach (var group in groups)
        {
            await AddGroup(group);
        }
        foreach (var member in members)
        {
            await AddMember(member);
        }
        foreach (var servicePrincipal in servicePrincipals)
        {
            await AddServicePrincipal(servicePrincipal);
        }
        Valid = true;
    }
}

public class RepositoryCache(ProjectCache project, GitRepository repository)
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

    public async Task Regenerate()
    {
        IEnumerable<string> tokens =
        [
            $"repoV2/{Repository.Id}",
            $"repoV2/{project.Project.Id}",
        ];
        foreach (var token in tokens)
        {
            var acls = await AzureDevops.GetRepoAcl(token);
            foreach (var acl in acls)
            {
                AddAcl(acl.Key, acl.Value, token);
            }            
        }

        Valid = true;
    }
}

public class PipelineCache(TeamProjectReference project, Pipeline pipeline)
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
    )
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
    )
{
    public readonly TeamProjectReference Project = project;
    public readonly Identity Identity = identity;
    public readonly GraphMember Member = member;
    public readonly List<GraphGroup> Memberships = memberships;
}

public class GroupCache(
    TeamProjectReference project,
    Identity identity,
    GraphGroup group)
{
    public readonly TeamProjectReference Project = project;
    public readonly Identity Identity = identity;
    public readonly GraphGroup Group = group;
}
public class ServicePrincipalCache(
    TeamProjectReference project,
    Identity identity,
    GraphServicePrincipal servicePrincipal)
{
    public readonly TeamProjectReference Project = project;
    public readonly Identity Identity = identity;
    public readonly GraphServicePrincipal ServicePrincipal = servicePrincipal;
}