import "prismjs/themes/prism-tomorrow.css";

import React from "react";
import { Provider } from "react-redux";
import { createStore } from "./src/state";

const store = createStore();

export function wrapRootElement({ element }) {
  return <Provider store={store}>{element}</Provider>;
}
