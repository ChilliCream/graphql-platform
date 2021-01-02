import React, { FC } from "react";
import styled from "styled-components";
import { useTabs } from "./tabs";

export interface PanelProps {
    value: string;
}

export const Panel: FC<PanelProps> = props => {
    const { activeTab } = useTabs();

    return activeTab === props.value ? <Container>{props.children}</Container> : null;
};

const Container = styled.div`
  margin-top: 0.5rem;
`;