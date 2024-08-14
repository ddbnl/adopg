import {ISimpleListCell} from "azure-devops-ui/List";
import {ColumnMore, ISimpleTableCell} from "azure-devops-ui/Table";
import {renderExpandableTreeCell, renderTreeCell} from "azure-devops-ui/TreeEx";
import {ITreeItem, ITreeItemProvider, TreeItemProvider,} from "azure-devops-ui/Utilities/TreeItemProvider";
import {IconSize} from "azure-devops-ui/Icon";
import * as SDK from "azure-devops-extension-sdk";
import {CommonServiceIds, IProjectPageService} from "azure-devops-extension-api";
import {
    remediateAllPolicies,
    remediateAllViolations,
    remediateViolation
} from "./BackendCalls";
import {Pipeline} from "./Types";
import {getHost} from "azure-devops-extension-sdk";

export interface IPolicyTableItem extends ISimpleTableCell {
    policy: string;
    status: ISimpleListCell;
}

export const pipelineColumn = {
    id: "pipeline",
    name: "Pipeline",
    renderCell: renderExpandableTreeCell,
    width: 200,
};
export const policyColumn = {
    id: "policy",
    name: "Policy",
    renderCell: renderTreeCell,
    width: -100,
};
export const statusColumn = {
    id: "status",
    name: "Status",
    renderCell: renderTreeCell,
    width: 100,
};

export const moreColumn = new ColumnMore(target => {
    return {
        id: "sub-menu",
        items: [
            { 
                id: "reconcile",
                text: "Reconcile",
                
                onActivate: (_menuItem: any, _event: any) => {
                    OnReconcile(target)
                        .catch(console.error)
                },
            },
            { id: "exempt", text: "Exempt" },
        ],
    };
});

export const treeColumns = [pipelineColumn, statusColumn, policyColumn, moreColumn];

async function OnReconcile(menuItem: any): Promise<void> {
    
    const organization = getHost().name;
    const policyId = menuItem.underlyingItem.data.policy_id;
    const violationId = menuItem.underlyingItem.data.violation_id;
    const pipeline = menuItem.underlyingItem.data.pipeline;
    const client = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
    const project = await client.getProject();
    const user = SDK.getUser();
    if (policyId == "all" && violationId == "all") {
        await remediateAllPolicies(organization, project.name, pipeline,  user.id)
    } else if (violationId == "all") {
        await remediateAllViolations(organization, project.name, policyId, user.id)
    } else {
        await remediateViolation(organization, project.name, policyId, violationId, user.id)
    }
}

export function getItemProvider(pipelines: Pipeline[]): ITreeItemProvider<IPolicyTableItem> {

    const rootItems: Array<ITreeItem<IPolicyTableItem>> = [];

    for (const pipeline of pipelines) {
        let pipelineCompliant = true;
        for (const policy of pipeline.Policies) {
            let policyCompliant = true;
            let errorChildren: any = [];
            let i = 0;
            policy.Errors.forEach(error => {
                policyCompliant = false;
                pipelineCompliant = false;
                errorChildren.push(
                    {
                        data: {
                            pipeline: "",
                            policy: error.Description,
                            status: GetErrorStatus(),
                            policy_id: policy.Id,
                            violation_id: error.Id,
                        }
                    }
                )
                i += 1;
            })

            let policyChildren: any = [];
            policyChildren.push({
                data: {
                    pipeline: "",
                    policy: policy.Description,
                    status: policyCompliant ? GetOkStatus() : GetErrorStatus(),
                    policy_id: policy.Id,
                    violation_id: "all",
                },
                childItems: errorChildren,
            });
            rootItems.push({
                data: {
                    pipeline: pipeline.Name,
                    policy: "",
                    status: pipelineCompliant ? GetOkStatus() : GetErrorStatus(),
                    policy_id: "all",
                    violation_id: "all",
                },
                childItems: policyChildren,
            }); 
        }
    }
    return new TreeItemProvider<IPolicyTableItem>(rootItems);
}
    
function GetOkStatus() : any {
    return { iconProps: { iconName: "StatusCircleInner", size: IconSize.large, style: { color: '#107c10'}}}
}

function GetErrorStatus() : any {
    return { iconProps: { iconName: "StatusErrorFull", size: IconSize.large, style: { color: '#cd4a45'}}}
}
