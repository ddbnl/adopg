import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";

import "./policy-guard.scss";

import {Header} from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";

import { CommonServiceIds, IProjectPageService } from "azure-devops-extension-api";
import {IHeaderCommandBarItem} from "azure-devops-ui/HeaderCommandBar";
import PolicyTree from "./policy-tree";
import {useEffect} from "react";
import {Pipeline} from "./Types";
import {GetPolicies, RefreshBackend} from "./BackendCalls";
import {Simulate} from "react-dom/test-utils";
import load = Simulate.load;


const PolicyGuard = () => {
    
    const [policies, setPolicies] = React.useState<Pipeline[]>([]);
    const [project, setProject] = React.useState<string>();
    useEffect(() => {
        const inner = async () => {

            try {
                SDK.init().catch(console.error)

                SDK.ready().then(async () => {
                    const client = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
                    const organization = SDK.getHost();
                        await client.getProject().then(async context => {
                        setProject(context.name);
                        await RefreshBackend(organization.name).then(async () => {
                            await GetPolicies(organization.name, context.name).then(loadedPolicies => {
                                setPolicies(loadedPolicies);
                            });
                        });
                    });
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
            <Header title="Policy guard" commandBarItems={getCommandBarItems(project, setPolicies)}/>
            <div className="page-content">
                <PolicyTree policies={policies}/>
            </div>
        </Page>
    );
}

function getCommandBarItems(project: string, setPolicies: any): IHeaderCommandBarItem[] {
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
                    await onRefreshClicked(project, setPolicies);
                }
                refreshFunc()
                    .catch(console.error);
            },
        },
    ];
}

async function onRefreshClicked(project: string, setPolicies: any): Promise<void> {

    const organization = SDK.getHost();
    await RefreshBackend(organization.name).then(async () => {
        await GetPolicies(organization.name, project).then(async policies => {
            setPolicies(policies);
        });
    })
}


export default PolicyGuard