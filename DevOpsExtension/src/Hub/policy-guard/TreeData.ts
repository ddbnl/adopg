import {ISimpleListCell} from "azure-devops-ui/List";
import {ColumnMore, ISimpleTableCell} from "azure-devops-ui/Table";
import {renderExpandableTreeCell, renderTreeCell} from "azure-devops-ui/TreeEx";
import {ITreeItem, ITreeItemProvider, TreeItemProvider,} from "azure-devops-ui/Utilities/TreeItemProvider";
import {IconSize} from "azure-devops-ui/Icon";
import {Pipeline, remediatePolicy} from "./policy-guard";
import {IMenuItem} from "azure-devops-ui/Menu";
import * as SDK from "azure-devops-extension-sdk";
import {CommonServiceIds, IProjectPageService} from "azure-devops-extension-api";

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
                
                onActivate: (menuItem: any, event: any) => {
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
    const policyId = menuItem.underlyingItem.data.reconcile;
    const reconcileError = menuItem.underlyingItem.data.reconcile_error;
    const client = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
    const project = await client.getProject();
    const user = SDK.getUser();
    console.log("onReconcile", user);
    await remediatePolicy(project.name, policyId, reconcileError, user.id)
}

export function getItemProvider(pipelines: Pipeline[]): ITreeItemProvider<IPolicyTableItem> {

    const rootItems: Array<ITreeItem<IPolicyTableItem>> = [];

    for (const pipeline of pipelines) {
        let pipelineCompliant = true;
        for (const policy of pipeline.Policies) {
            let errorChildren: any = [];
            let i = 0;
            policy.Errors.forEach(error => {
                pipelineCompliant = false;
                errorChildren.push(
                    {
                        data: {
                            pipeline: "",
                            policy: error,
                            status: GetErrorStatus(),
                            reconcile: policy.Id,
                            reconcile_error: i,
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
                    status: policy.Compliant ? GetOkStatus() : GetErrorStatus(),
                },
                childItems: errorChildren,
            });
            rootItems.push({
                data: {
                    pipeline: pipeline.Name,
                    policy: "",
                    status: pipelineCompliant ? GetOkStatus() : GetErrorStatus(),
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
