"use client";

import React, { FC } from "react";

import { useTabs } from "./tabs";

export interface PanelProps {
  readonly value: string;
  readonly children: React.ReactNode;
}

export const Panel: FC<PanelProps> = (props) => {
  const { activeTab } = useTabs();

  return activeTab === props.value ? <>{props.children}</> : null;
};
