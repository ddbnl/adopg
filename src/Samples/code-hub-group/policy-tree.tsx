import * as React from "react";
import { getItemProvider, IPolicyTableItem, treeColumns } from "./TreeData";
import { Card } from "azure-devops-ui/Card";
import { Tree } from "azure-devops-ui/TreeEx";
import { ITreeItemProvider, ITreeItemEx } from "azure-devops-ui/Utilities/TreeItemProvider";
import {Pipeline} from "./code-hub-group";

interface PolicyProps {
    policies: Pipeline[];
}

const PolicyTree = ({policies}: PolicyProps) => {

    const itemProvider = getItemProvider(policies);
    return (
        <Card
            className="flex-grow bolt-card-no-vertical-padding bolt-table-card"
            contentProps={{ contentPadding: false }}
        >
            <Tree<IPolicyTableItem>
                ariaLabel="Basic tree"
                columns={treeColumns}
                itemProvider={itemProvider}
                onToggle={(event, treeItem: ITreeItemEx<IPolicyTableItem>) => {
                    itemProvider.toggle(treeItem.underlyingItem);
                }}
                scrollable={true}
            />
        </Card>
    );
}

export default PolicyTree;