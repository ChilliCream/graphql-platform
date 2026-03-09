"use client";

import React, { createContext, ReactNode, useContext, useMemo } from "react";
import { Provider } from "react-redux";

import createStore from "@/state";
import { initialState as workshopsInitialState } from "@/state/workshops/workshops.state";
import { StyledComponentsRegistry } from "./registry";

export interface LatestBlogPost {
  title: string;
  path: string;
  date: string;
  readingTime: string;
  featuredImage?: string;
}

const LatestBlogPostContext = createContext<LatestBlogPost | null>(null);

export function useLatestBlogPost() {
  return useContext(LatestBlogPostContext);
}

interface ProvidersProps {
  children: ReactNode;
  latestBlogPost?: LatestBlogPost | null;
}

export function Providers({ children, latestBlogPost }: ProvidersProps) {
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
        workshops: workshopsInitialState,
      }),
    []
  );

  return (
    <Provider store={store}>
      <LatestBlogPostContext.Provider value={latestBlogPost ?? null}>
        <StyledComponentsRegistry>{children}</StyledComponentsRegistry>
      </LatestBlogPostContext.Provider>
    </Provider>
  );
}
