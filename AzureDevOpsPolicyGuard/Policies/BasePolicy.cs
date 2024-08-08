using AzureDevOpsPolicyGuard.Support;
using Microsoft.Azure.Pipelines.WebApi;

namespace AzureDevOpsPolicyGuard.Policies;

public abstract class BasePolicy
{
    public readonly Guid Id;
    public readonly string Project;
    public readonly int PipelineId;
    protected bool IsCompliant;
    protected bool IsChecked;
    protected DateTimeOffset LastChecked;
    protected List<string> ComplianceFailures = [];

    public BasePolicy(string project, int pipelineId)
    {
        Id = Guid.NewGuid();
        Project = project;
        PipelineId = pipelineId;
    }

    public abstract Task Remediate();

    public bool GetCompliant()
    {
        return IsChecked && IsCompliant;
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

    public List<string> GetComplianceFailures()
    {
        return ComplianceFailures;
    }
    
    public abstract void CheckCompliance();
}