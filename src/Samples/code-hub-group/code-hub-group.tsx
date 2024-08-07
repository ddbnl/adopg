import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";

import "./code-hub-group.scss";

import {Header} from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";

import { CommonServiceIds, IProjectPageService } from "azure-devops-extension-api";
import {GitServiceIds, IVersionControlRepositoryService} from "azure-devops-extension-api/Git/GitServices";
import {IHeaderCommandBarItem} from "azure-devops-ui/HeaderCommandBar";
import PolicyTree from "./policy-tree";
import {useEffect} from "react";


const CodeHubGroup = () => {
    
    const [policies, setPolicies] = React.useState<Pipeline[]>([]);
    useEffect(() => {
        const inner = async () => {

            try {
                console.log("Component did mount, initializing SDK...");
                SDK.init().catch(console.error)

                SDK.ready().then(async () => {
                    console.log("SDK is ready, loading project context...");
                    const client = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
                    const repoClient = await SDK
                        .getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService);
                    const organization = SDK.getHost();
                    const policies = await client.getProject().then(async context => {
                        return repoClient.getCurrentGitRepository().then(async repo => {
                            return connectBackend(organization.name).then(async () => {
                                return RefreshBackend().then(async () => {
                                    return await GetPolicies(context.name, repo.name);
                                });
                            });
                        });
                    })
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

async function GetPolicies(project: string, repo: string) : Promise<Pipeline[]> {
    const requestOptions = {
        method: 'GET',
        headers: {'Content-Type': 'application/json'},
    }
    const url =`http://localhost:5214/projects/${project}/policies`;
    console.log("AAAAAAAAAAAAADSFF")
    const response = await fetch(url, requestOptions);
    const json: object = await response.json();
    console.log(json);
    
    let pipelines: Pipeline[] = [];

    
    for (const [pipelineName, pipelineData] of Object.entries(json)) {
        console.log("1")
        
        let policies: Policy[] = [];
        for (const policyJson of pipelineData) {
            console.log("2")
            policies.push({
                Description: policyJson['description'],
                Compliant: policyJson['compliant'],
                LastChecked: policyJson['lastChecked'],
                Errors: policyJson['errors'],
            });
            console.log("3")
        }
        console.log("4")
        const pipeline: Pipeline = {
            Name: pipelineName,
            Policies: policies,
        }
        console.log("5")
        pipelines.push(pipeline);
    }
    console.log("6")
    return pipelines;
}

export default CodeHubGroup

export interface Pipeline {
    Name: string;
    Policies: Policy[];
}

export interface Policy {
    Description: string;
    Compliant: boolean;
    LastChecked: string;
    Errors: [string];
}