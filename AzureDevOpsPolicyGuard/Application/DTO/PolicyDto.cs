namespace AzureDevOpsPolicyGuard.DTO;

public class PolicyDto
{
    public Guid Id { get; set; }
    public string Pipeline { get; set; }
    public string Description { get; set; }
    public bool Compliant { get; set; }
    public DateTimeOffset LastChecked { get; set; }
    public IEnumerable<ViolationDto> Errors { get; set; }
}