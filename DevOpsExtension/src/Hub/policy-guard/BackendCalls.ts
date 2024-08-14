import {ParsePipelinesFromJson, Pipeline} from "./Types";
import {getAppToken} from "azure-devops-extension-sdk";
import {IRequestOptions} from "azure-devops-node-api/interfaces/common/VsoBaseInterfaces";


export async function RefreshBackend(organization: string): Promise<void> {

    const requestOptions = await _GetRequest('POST');
    await _Fetch(organization, `refresh`, requestOptions);
}


export async function remediateAllPolicies(organization: string,  project: string, pipeline: string, userDescriptor: string): Promise<void> {

    const body = {
        descriptor: userDescriptor,
    }
    const requestOptions = await _GetRequest('POST', body);
    const url = `projects/${project}/pipelines/${pipeline}/remediate`;
    await _Fetch(organization, url, requestOptions)
}


export async function remediateAllViolations(organization: string,  project: string,  policyId: string, userDescriptor: string): Promise<void> {
    const body = {
        descriptor: userDescriptor,
    }
    const requestOptions = await _GetRequest('POST', body);
    const url = `projects/${project}/remediate/${policyId}`;
    await _Fetch(organization, url, requestOptions)
}


export async function remediateViolation(
    organization: string,  project: string,  policyId: string, violationId: string, userDescriptor: string): Promise<void> {
    
    const body = {
        descriptor: userDescriptor,
    }
    const requestOptions = await _GetRequest('POST', body);
    const url = `projects/${project}/remediate/${policyId}/${violationId}`;
    await _Fetch(organization, url, requestOptions)
}


export async function GetPolicies(organization: string,  project: string) : Promise<Pipeline[]> {
    const requestOptions = await _GetRequest('GET');
    const url =`projects/${project}`;
    const response = await _Fetch(organization, url, requestOptions);
    const json: object = await response.json();

    return ParsePipelinesFromJson(json);
}


async function _GetRequest(method: string, body: object = {}): Promise<RequestInit> {
    
    if (method == 'GET') {
        return {
            method: method,
            headers: await _GetHeader(),
        }
    } else {
        return {
            method: method,
            headers: await _GetHeader(),
            body: JSON.stringify(body)
        }
    }

}


async function _GetHeader(): Promise<HeadersInit> {
    return {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${await getAppToken()}`
    }
}

async function _Fetch(organization: string,  url: string, requestOptions: IRequestOptions): Promise<Response> {
    console.log(requestOptions);
    return await fetch(`http://localhost:5214/api/${organization}/policies/${url}`, requestOptions);
}