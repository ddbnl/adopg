import "azure-devops-ui/Core/override.css";
import "es6-promise/auto";
import ReactDOM from 'react-dom'
import "./Common.scss";
import PolicyGuard from "./Hub/policy-guard/policy-guard";
import React from "react";

ReactDOM.render(
    <PolicyGuard/>,
    document.getElementById("root"),
)