"use client";

import React, { FC } from "react";
import styled from "styled-components";

import { GridCard, GridSection } from "@/components/redesign-system/grid";

import { AgentsLoopDiagram } from "../AgentsLoopDiagram";

// Section 03 (Grid): the Loop. The page's intellectual property and visual
// climax. Renders AgentsLoopDiagram inside a noPadding GridCard so the
// diagram sits flush against a strict 1px hairline frame. The accent wash
// the underlying Band provides reads as the amber tint inside the card,
// which is how Vercel's dashboard composites read on grid pages: the chrome
// stays neutral, the visualization holds the color.

const LoopShell = styled.div`
  /* Neutralize the page-level padding the inherited Band applies, since
     GridCard noPadding already handles inset. The internal stage strip and
     SVG keep their own breathing room. */
  > section {
    padding: 0;
  }
  > section > .cc-ag-loop-band {
    padding: clamp(48px, 6vw, 96px);
  }
`;

export const AgentsGridLoop: FC = () => {
  return (
    <GridSection variant="default">
      <GridCard noPadding>
        <LoopShell>
          <AgentsLoopDiagram />
        </LoopShell>
      </GridCard>
    </GridSection>
  );
};
