using AzureDevOpsPolicyGuard.DTO;
using AzureDevOpsPolicyGuard.Policies;
using AzureDevOpsPolicyGuard.Support;
using DefaultNamespace;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOpsPolicyGuard.Controllers;

[ApiController]
[Route("/api/policies")]
public class PolicyController : ControllerBase
{
    
    [HttpPost]
    [Route("/connect")]
    public IActionResult Connect([FromQuery] string organization)
    {
        AzureDevops.SetOrganization(organization);
        var result = new StatusDto
        {
            Status = $"Connected to {organization}"
        };
        Console.WriteLine($"Connected to {organization}");
        return Ok(result);
    }

    [HttpPost]
    [Route("/refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!OrganizationCache.Valid)
        {
            await OrganizationCache.Regenerate();
            RepoNoDeleteRepository.PopulatePolicies();
            RepoNoDeleteRepository.UpdatePolicies();
        }

        var result = new StatusDto
        {
            Status = "Refreshed"
        };
        return Ok(result);
    }

    [HttpGet]
    [Route("/projects/{project}/policies")]
    public IActionResult GetAllPolicies(string project)
    {
        Dictionary<string, List<PolicyDto>> pipelines = new();
        foreach (var policy in RepoNoDeleteRepository.Policies)
        {
            var policyDto = new PolicyDto
            {
                Id = policy.Id,
                Description = policy.GetDescription(),
                Compliant = policy.GetCompliant(),
                LastChecked = policy.GetLastChecked(),
                Errors = policy.GetComplianceFailures()
            };
            var pipelineName = policy.PipelineName();
            if (!pipelines.ContainsKey(pipelineName))
            {
                pipelines.Add(pipelineName, [policyDto]);
            }
            else
            {
                pipelines[pipelineName].Add(policyDto);
            }
        }
        return Ok(pipelines);
    }
    
    [HttpPost]
    [Route("/projects/{project}/policies/{policyId}/remediate")]
    public IActionResult RemediatePolicy(string project, Guid policyId, UserDto user)
    {
        if (!IsProjectAdmin.Evaluate(project, "Project Administrators", user.Descriptor))
        {
            return Unauthorized("You are not a project administrator.");
        }
        var policy = RepoNoDeleteRepository.GetById(policyId);
        policy.Remediate();
        return Ok("Remediation started.");
    }
}