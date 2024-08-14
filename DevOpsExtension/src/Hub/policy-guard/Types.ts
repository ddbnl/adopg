
export interface Pipeline {
    Name: string;
    Policies: Policy[];
}

export interface Policy {
    Id: string;
    Description: string;
    Compliant: boolean;
    LastChecked: string;
    Errors: Array<Violation>;
}
export interface Violation {
    Id: string;
    Description: string;
}


export function ParsePipelinesFromJson(json: object): Pipeline[] {

    let pipelines: Pipeline[] = [];
    for (const [pipelineName, pipelineData] of Object.entries(json)) {

        let policies: Policy[] = [];
        for (const policyJson of pipelineData) {
            let violations: Array<Violation> = [];
            for (const violationJson of policyJson['errors']) {
                violations.push({
                    Id: violationJson['id'],
                    Description: violationJson['description'],
                });
            }
            policies.push({
                Id: policyJson['id'],
                Description: policyJson['description'],
                Compliant: policyJson['compliant'],
                LastChecked: policyJson['lastChecked'],
                Errors: violations,
            });
        }
        const pipeline: Pipeline = {
            Name: pipelineName,
            Policies: policies,
        }
        pipelines.push(pipeline);
    }
    return pipelines;
}
