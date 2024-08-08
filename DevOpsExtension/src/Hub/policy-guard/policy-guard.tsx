import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";

import "./policy-guard.scss";

import {Header} from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";

import { CommonServiceIds, IProjectPageService } from "azure-devops-extension-api";
import {IHeaderCommandBarItem} from "azure-devops-ui/HeaderCommandBar";
import PolicyTree from "./policy-tree";
import {useEffect} from "react";


const PolicyGuard = () => {
    
    const [policies, setPolicies] = React.useState<Pipeline[]>([]);
    useEffect(() => {
        const inner = async () => {

            try {
                console.log("Initializing SDK...");
                SDK.init().catch(console.error)

                SDK.ready().then(async () => {
                    console.log("SDK is ready...");
                    const client = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
                    const organization = SDK.getHost();
                    const policies = await client.getProject().then(async context => {
                        return connectBackend(organization.name).then(async () => {
                            return RefreshBackend().then(async () => {
                                return await GetPolicies(context.name);
                            });
                        });
                    });
                    setPolicies(policies);
                }).catch((error) => {
                    console.error("SDK ready failed: ", error);
                });
            } catch (error) {
                console.error("Error during SDK initialization or project context loading: ", error);
            }
            await SDK.notifyLoadSucceeded();
        }
        inner()
            .catch(console.error);

    }, [])

    return (
        <Page className="sample-hub flex-grow">
            <Header title="Policy guard" commandBarItems={getCommandBarItems()}/>
            <div className="page-content">
                <PolicyTree policies={policies}/>
            </div>
        </Page>
    );
}

function getCommandBarItems(): IHeaderCommandBarItem[] {
    return [
        {
            id: "refresh",
            text: "Refresh",
            isPrimary: true,
            iconProps: {
                iconName: "Refresh",
            },
            onActivate: () => {
                const refreshFunc = async () => {
                    await onRefreshClicked();
                }
                refreshFunc()
                    .catch(console.error);
            },
        },
    ];
}

async function onRefreshClicked(): Promise<void> {

    const organization = SDK.getHost();
    await connectBackend(organization.name);
    await RefreshBackend();
}

async function connectBackend(organization: string): Promise<void> {
    const requestOptions = {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
    }
    await fetch(`http://localhost:5214/connect?organization=${organization}`, requestOptions)
}

async function RefreshBackend(): Promise<void> {
    const requestOptions = {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
    }
    await fetch('http://localhost:5214/refresh', requestOptions)
}

export async function remediatePolicy(project: string,  policyId: string,  userDescriptor: string): Promise<void> {
    const body = {
        descriptor: userDescriptor,
    }
    const requestOptions = {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify(body),
    }
    await fetch(`http://localhost:5214/projects/${project}/policies/${policyId}/remediate`, requestOptions)
}
async function GetPolicies(project: string) : Promise<Pipeline[]> {
    const requestOptions = {
        method: 'GET',
        headers: {'Content-Type': 'application/json'},
    }
    const url =`http://localhost:5214/projects/${project}/policies`;
    const response = await fetch(url, requestOptions);
    const json: object = await response.json();
    
    let pipelines: Pipeline[] = [];

    
    for (const [pipelineName, pipelineData] of Object.entries(json)) {
        
        let policies: Policy[] = [];
        for (const policyJson of pipelineData) {
            policies.push({
                Id: policyJson['id'],
                Description: policyJson['description'],
                Compliant: policyJson['compliant'],
                LastChecked: policyJson['lastChecked'],
                Errors: policyJson['errors'],
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

export default PolicyGuard

export interface Pipeline {
    Name: string;
    Policies: Policy[];
}

export interface Policy {
    Id: string;
    Description: string;
    Compliant: boolean;
    LastChecked: string;
    Errors: [string];
}