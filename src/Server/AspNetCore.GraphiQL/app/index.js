import React from "react";
import ReactDOM from "react-dom";
import GraphiQL from "graphiql";
import "whatwg-fetch";
import "es6-promise/auto";

import "graphiql/graphiql.css";

import {
  parameters,
  onEditQuery,
  onEditVariables,
  onEditOperationName,
} from "./parameters";
import settings from "./settings";
import fetcherFactory from "./fetcher";

const graphQLFetcher = fetcherFactory(settings);

ReactDOM.render(
  <GraphiQL
    fetcher={graphQLFetcher}
    query={parameters.query}
    variables={parameters.variables}
    operationName={parameters.operationName}
    onEditQuery={onEditQuery}
    onEditVariables={onEditVariables}
    onEditOperationName={onEditOperationName}
  />,
  document.getElementById("graphiql"),
);

console.log("hey");
