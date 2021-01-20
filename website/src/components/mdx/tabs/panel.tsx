import React, { FC } from "react";
import styled from "styled-components";
import { useTabs } from "./tabs";

export interface PanelProps {
  value: string;
}

export const Panel: FC<PanelProps> = props => {
  const { activeTab } = useTabs();

  return activeTab === props.value ? <><Spacer />{props.children}</> : null;
};

const Spacer = styled.div`
  margin-top: 0.5rem;
`;