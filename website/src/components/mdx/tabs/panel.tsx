import React, { FunctionComponent } from "react";
import { useTabs } from "./tabs";

export interface PanelProps {
  value: string;
}

export const Panel: FunctionComponent<PanelProps> = props => {
  const { activeTab } = useTabs();

  return activeTab === props.value ? <>{props.children}</> : null;
};