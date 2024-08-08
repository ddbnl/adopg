# Azure DevOps Policy guard

Implements, audits and enforces policies in Azure DevOps. If policies are not met, pipelines cannot run. Auto-remediation
is available. Currently in the exploration stage. Contains the Azure DevOps extension and an API server that 
implements and scans the actual policies.

# Policies:
- Nobody can have delete permissions on repo containing pipeline
