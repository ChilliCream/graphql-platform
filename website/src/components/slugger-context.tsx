import GithubSlugger from "github-slugger";
import React, { FunctionComponent, useContext } from "react";

const slugger = new GithubSlugger();

const SluggerContext = React.createContext(slugger);

export const SluggerContextProvider: FunctionComponent = ({ children }) => {
  return (
    <SluggerContext.Provider value={slugger}>
      {children}
    </SluggerContext.Provider>
  );
};

export function useSlugger() {
  return useContext(SluggerContext);
}
