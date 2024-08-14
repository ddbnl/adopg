using AzureDevOpsPolicyGuard.Application.Common.Authorization;
using AzureDevOpsPolicyGuard.Application.Services;
using AzureDevOpsPolicyGuard.Domain.Mappings;
using AzureDevOpsPolicyGuard.DTO;
using AzureDevOpsPolicyGuard.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AzureDevOpsPolicyGuard.Application.Controllers;


[ApiController]
[Authorize]
[Route("/api/{organization}/policies")]
public class PolicyController : ControllerBase
{
    public PolicyController(
        IAzureDevopsService azureDevopsService,
        IRepoNoDeleteRepository repoNoDeleteRepository,
        IOrganizationCacheService organizationCacheService)
    {
        _azureDevopsService = azureDevopsService;
        _repoNoDeleteRepository = repoNoDeleteRepository;
        _organizationCache = organizationCacheService;
        
    }
    
    private IRepoNoDeleteRepository _repoNoDeleteRepository;
    private IAzureDevopsService _azureDevopsService;
    private IOrganizationCacheService _organizationCache;
    
    [HttpPost]
    [Authorize]
    [Route("refresh")]
    public async Task<IActionResult> Refresh(string organization)
    {
        _azureDevopsService.SetOrganization(organization);
        await _organizationCache.Regenerate(_azureDevopsService);
        _repoNoDeleteRepository.PopulatePolicies(_organizationCache);
        _repoNoDeleteRepository.UpdatePolicies();
        var result = new StatusDto
        {
            Status = "Refreshed"
        };
        Console.WriteLine($"Refresh policies: {_repoNoDeleteRepository.GetPolicies().Count()}");
        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    [Route("projects/{project}")]
    public IActionResult GetAllPolicies(string organization, string project)
    {
        Dictionary<string, List<PolicyDto>> pipelines = new();
        Console.WriteLine($"Get policies: {_repoNoDeleteRepository.GetPolicies().Count()}");
        foreach (var policy in _repoNoDeleteRepository.GetPolicies())
        {
            PolicyDto policyDto = policy.ToDto(_organizationCache);
            if (!pipelines.ContainsKey(policyDto.Pipeline))
            {
                pipelines.Add(policyDto.Pipeline, [policyDto]);
            }
            else
            {
                pipelines[policyDto.Pipeline].Add(policyDto);
            }
        }
        return Ok(pipelines);
    }
    
    [HttpPost]
    [Authorize]
    [Route("projects/{project}/pipelines/{pipeline}/remediate")]
    public async Task<IActionResult> RemediateAllPolicies(
        string organization,
        string project,
        string pipeline,
        UserDto user
        )
    {
        if (!IsProjectAdmin.Evaluate(project, "Project Administrators", user.Descriptor, _organizationCache))
        {
            return Unauthorized("You are not a project administrator.");
        }

        _azureDevopsService.SetOrganization(organization);
        var toRemediate = _repoNoDeleteRepository
            .GetPolicies()
            .Where(c => c.Project.Project.Name == project && c.GetPipelineName(_organizationCache) == pipeline);
        foreach (var policy in toRemediate)
        {
            await policy.RemediateAll(_azureDevopsService);
        }
        return Ok("Remediation started.");
    }
    
    [HttpPost]
    [Authorize]
    [Route("projects/{project}/remediate/{policyId:guid}")]
    public async Task<IActionResult> RemediatePolicy(
        string organization,
        string project,
        Guid policyId,
        UserDto user
        )
    {
        if (!IsProjectAdmin.Evaluate(project, "Project Administrators", user.Descriptor, _organizationCache))
        {
            return Unauthorized("You are not a project administrator.");
        }

        _azureDevopsService.SetOrganization(organization);
        var toRemediate = _repoNoDeleteRepository
            .GetPolicies()
            .Where(c => c.Project.Project.Name == project);
        foreach (var policy in toRemediate)
        {
            await policy.RemediateAll(_azureDevopsService);
        }
        return Ok("Remediation started.");
    }
    
    [HttpPost]
    [Authorize]
    [Route("projects/{project}/remediate/{policyId:guid}/{violationId:guid}")]
    public async Task<IActionResult> RemediatePolicyViolation(
        string organization,
        string project,
        Guid policyId,
        Guid violationId,
        UserDto user
        )
    {
        if (!IsProjectAdmin.Evaluate(project, "Project Administrators", user.Descriptor, _organizationCache))
        {
            return Unauthorized("You are not a project administrator.");
        }
        _azureDevopsService.SetOrganization(organization);
        var policy = _repoNoDeleteRepository.GetById(policyId);
        await policy.Remediate(_azureDevopsService, violationId);
        return Ok("Remediation started.");
    }
}