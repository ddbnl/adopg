using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Support;
using Microsoft.Azure.Pipelines.WebApi;

namespace AzureDevOpsPolicyGuard.Domain.Policies;

public abstract class BasePolicy
{
    public readonly Guid Id;
    public readonly ProjectCache Project;
    public readonly int PipelineId;
    public readonly List<BasePolicyViolation> Violations = [];
    protected bool IsChecked;
    protected DateTimeOffset LastChecked;

    public BasePolicy(ProjectCache project, int pipelineId)
    {
        Id = Guid.NewGuid();
        Project = project;
        PipelineId = pipelineId;
    }
    
    public string GetPipelineName(IOrganizationCacheService organizationCacheService)
    {
        return organizationCacheService
            .GetProjects()
            .First(c => c.Project.Name == Project.Project.Name)
            .Pipelines.First(c => c.Pipeline.Id == PipelineId)
            .Pipeline.Name;
    }

    public async Task Remediate(IAzureDevopsService azureDevopsService, Guid violationId)
    {
        await Violations.First(c => c.Id == violationId).Remediate(azureDevopsService);
    }
    public async Task RemediateAll(IAzureDevopsService azureDevopsService)
    {
        foreach (var violation in Violations)
        {
            await violation.Remediate(azureDevopsService);
        }
    }
    
    public bool GetCompliant()
    {
        return IsChecked && Violations.Count == 0;
    }
    
    public abstract string GetDescription();

    public bool GetChecked()
    {
        return IsChecked;
    }

    public DateTimeOffset GetLastChecked()
    {
        return LastChecked;
    }

    public IEnumerable<string> GetComplianceFailures()
    {
        return Violations.Select(c => c.Describe()).AsEnumerable();
    }
    
    public abstract void CheckCompliance();
}