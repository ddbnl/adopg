import * as React from "react";
import * as SDK from "azure-devops-extension-sdk";

import "./code-hub-group.scss";

import {Header} from "azure-devops-ui/Header";
import { Page } from "azure-devops-ui/Page";

import { showRootComponent } from "../../Common";
import { CommonServiceIds, IProjectPageService } from "azure-devops-extension-api";
import {GitServiceIds, IVersionControlRepositoryService} from "azure-devops-extension-api/Git/GitServices";
import {IHeaderCommandBarItem} from "azure-devops-ui/HeaderCommandBar";
import {ITreeColumn, renderTreeCell, Tree} from "azure-devops-ui/TreeEx";
import {ArrayItemProvider, IItemProvider} from "azure-devops-ui/Utilities/Provider";
import {TreeItemProvider} from "azure-devops-ui/Utilities/TreeItemProvider";
import {
    ColumnFill, ISimpleTableCell,
    ITableColumn,
    renderEmptyCell,
    renderSimpleCell,
    Table,
    TableColumnLayout
} from "azure-devops-ui/Table";
import {ItemsObserver} from "azure-devops-ui/Observer";
import {ObservableValue} from "azure-devops-ui/Core/Observable";

interface ICodeHubGroup { 
    projectName: string;
    repoName: string;
    orgName: string;
    
}

class CodeHubGroup extends React.Component<{}, ICodeHubGroup> {

    constructor(props: {}) {
        super(props);
        this.state = {projectName: "Loading..", repoName: "Loading..", orgName: "Loading.."};
    }

    public componentDidMount() {
        try {
            console.log("Component did mount, initializing SDK...");
            SDK.init().catch(console.error)

            SDK.ready().then(async () => {
                console.log("SDK is ready, loading project context...");
                await this.loadProjectContext();
            }).catch((error) => {
                console.error("SDK ready failed: ", error);
            });
        } catch (error) {
            console.error("Error during SDK initialization or project context loading: ", error);
        }
    }

    public render(): JSX.Element {
        return (
            <Page className="sample-hub flex-grow">
                <Header title="Policy guard" commandBarItems={this.getCommandBarItems()}/>
                <div className="page-content">
                    <Table columns={tableDefinition} itemProvider={getItems()} role={"table"}/>
                </div>
            </Page>
        );
    }

    private async loadProjectContext(): Promise<void> {
        try {
            const client = await SDK.getService<IProjectPageService>(CommonServiceIds.ProjectPageService);
            const context = await client.getProject();
            const repoClient = await SDK.getService<IVersionControlRepositoryService>(GitServiceIds.VersionControlRepositoryService);
            const repo = await repoClient.getCurrentGitRepository();
            const organization = SDK.getHost();

            this.setState({
                projectName: (context != null) ? context.name : "Loading..",
                repoName: (repo != null) ? repo.name : "Loading..",
                orgName: organization.name,
            });
            SDK.notifyLoadSucceeded();
        } catch (error) {
            console.error("Failed to load project context: ", error);
        }
    }

    private getCommandBarItems(): IHeaderCommandBarItem[] {
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
                        await this.onRefreshClicked();
                    }
                    refreshFunc()
                        .catch(console.error);
                },
            },
        ];
    }

    private async onRefreshClicked(): Promise<void> {

        await this.connectBackend();
        await this.RefreshBackend();
    }

    private async connectBackend(): Promise<void> {
        const requestOptions = {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
        }
        await fetch(`http://localhost:5214/connect?organization=${this.state.orgName}`, requestOptions)
    }

    private async RefreshBackend(): Promise<void> {
        const requestOptions = {
            method: 'POST',
            headers: {'Content-Type': 'application/json'},
        }
        await fetch('http://localhost:5214/refresh', requestOptions)
    }
}


const tableDefinition = [
    {
        columnLayout: TableColumnLayout.singleLinePrefix,
        id: 'name',
        name: 'Status',
        readonly: true,
        renderCell: renderSimpleCell,
        width: new ObservableValue(200),
    },
    {
        id: 'policy',
        name: 'Policy',
        readonly: true,
        renderCell: renderSimpleCell,
        width: new ObservableValue(600),
    },
    {
        id: 'actions',
        name: 'Actions',
        readonly: true,
        renderCell: renderSimpleCell,
        width: new ObservableValue(600),
    },
    ColumnFill,
];

interface FieldTableItem extends ISimpleTableCell {
    name: string;
    policy: string;
    actions: string;
}

interface FieldsTableProps {
    itemProvider: IItemProvider<FieldTableItem>;
}

function getItems(): IItemProvider<FieldTableItem> {
    return new ArrayItemProvider<FieldTableItem>([
            {
                name: "een",
                policy: "een",
                actions: "een",
            },
            {
                name: "twee",
                policy: "twee",
                actions: "twee",
            },
            {
                name: "drie",
                policy: "drie",
                actions: "drie",
            }
        ])
}
    

showRootComponent(<CodeHubGroup />);