import {ISimpleListCell} from "azure-devops-ui/List";
import {ColumnMore, ISimpleTableCell} from "azure-devops-ui/Table";
import {renderExpandableTreeCell, renderTreeCell} from "azure-devops-ui/TreeEx";
import {ITreeItem, ITreeItemProvider, TreeItemProvider,} from "azure-devops-ui/Utilities/TreeItemProvider";
import {IconSize} from "azure-devops-ui/Icon";
import {Pipeline, Policy} from "./code-hub-group";
import {Simulate} from "react-dom/test-utils";
import error = Simulate.error;

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
export const moreColumn = new ColumnMore(() => {
    return {
        id: "sub-menu",
        items: [
            { id: "submenu-one", text: "Reconcile" },
            { id: "submenu-two", text: "Exempt" },
        ],
    };
});

export const treeColumns = [pipelineColumn, statusColumn, policyColumn, moreColumn];

export function getItemProvider(pipelines: Pipeline[]): ITreeItemProvider<IPolicyTableItem> {

    const rootItems: Array<ITreeItem<IPolicyTableItem>> = [];

    for (const pipeline of pipelines) {
        let pipelineCompliant = true;
        for (const policy of pipeline.Policies) {
            let errorChildren: any = [];
            policy.Errors.forEach(error => {
                pipelineCompliant = false;
                errorChildren.push(
                    {
                        data: {
                            pipeline: "",
                            policy: error,
                            status: GetErrorStatus(),
                        }
                    }
                )
                return errorChildren
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
