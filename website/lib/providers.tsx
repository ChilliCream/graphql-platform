"use client";

import React, { ReactNode, useMemo } from "react";
import { Provider } from "react-redux";

import createStore from "@/state";
import { StyledComponentsRegistry } from "./registry";

export function Providers({ children }: { children: ReactNode }) {
  const store = useMemo(
    () =>
      createStore({
        common: {
          searchQuery: "",
          showAside: false,
          showPromo: false,
          showTOC: false,
          yScrollPosition: 0,
          articleViewportHeight: "94vh",
        },
        workshops: [],
      }),
    []
  );

  return (
    <Provider store={store}>
      <StyledComponentsRegistry>{children}</StyledComponentsRegistry>
    </Provider>
  );
}
