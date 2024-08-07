import "azure-devops-ui/Core/override.css";
import "es6-promise/auto";
import ReactDOM from 'react-dom'
import "./Common.scss";
import CodeHubGroup from "./Samples/code-hub-group/code-hub-group";
import React from "react";

ReactDOM.render(
    <CodeHubGroup/>,
    document.getElementById("root"),
)