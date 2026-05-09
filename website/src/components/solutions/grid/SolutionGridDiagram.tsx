"use client";

import React, { FC } from "react";
import styled from "styled-components";

import {
  GridCard,
  GridSection,
  GRID_TOKENS,
} from "@/components/redesign-system/grid";
import type { DiagramKind } from "@/data/solutions/types";

import { ConceptDiagramBody, DIAGRAM_TITLES } from "../ConceptDiagram";

interface SolutionGridDiagramProps {
  readonly kind: DiagramKind;
}

// Archetype J (inverted band). Full-bleed inverted GridSection holds a
// noPadding GridCard which contains the full-width diagram body. Reuses
// the per-variant diagrams already shipped by the default renderer
// (federation/polyglot/agents/single-graph/event-bus/compliance) by
// importing the body-only entry point so we drop the Band wrapper.
export const SolutionGridDiagram: FC<SolutionGridDiagramProps> = ({ kind }) => {
  return (
    <GridSection variant="inverted" hairlineBottom>
      <Head>
        <span className="cc-grid-eyebrow">The shape</span>
        <h2 className="cc-grid-h2">{DIAGRAM_TITLES[kind]}</h2>
      </Head>
      <GridCard noPadding>
        <Canvas>
          <ConceptDiagramBody kind={kind} />
        </Canvas>
      </GridCard>
    </GridSection>
  );
};

const Head = styled.div`
  text-align: center;
  max-width: 760px;
  margin: 0 auto 56px;

  .cc-grid-h2 {
    color: ${GRID_TOKENS.inkPrimary};
  }
`;

const Canvas = styled.div`
  width: 100%;
  padding: clamp(28px, 4vw, 56px);
  color: ${GRID_TOKENS.inkPrimary};

  svg {
    width: 100%;
    height: auto;
    max-height: 560px;
    display: block;
  }
`;
