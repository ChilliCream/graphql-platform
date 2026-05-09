"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { SolutionsRoot } from "../SolutionsRoot";
import { SolutionThemes, SolutionSlug } from "./SolutionThemes";

interface SolutionsCinematicRootProps {
  readonly slug: string;
  readonly children: ReactNode;
  readonly className?: string;
}

// SolutionsCinematicRoot wraps the standard SolutionsRoot and paints a
// per-slug ambient background behind the section stack. Each of the seven
// solution slugs renders its own concept (typography mosaic, single planet,
// honeycomb mesh, triangular lattice, event pulse-stream, guilloche rosette,
// heraldic seal) so the cinematic pages read as distinct siblings rather than
// recolours of the same chart.
//
// Tonal palette, typography, button system, and section CSS stay 1:1 with
// the default variant by inheriting from `SolutionsRoot`; the cinematic
// chrome is purely the background layer.
export const SolutionsCinematicRoot: FC<SolutionsCinematicRootProps> = ({
  slug,
  children,
  className,
}) => (
  <Outer className={className}>
    <SolutionThemes slug={slug as SolutionSlug} />
    <Inner>{children}</Inner>
  </Outer>
);

const Outer = styled(SolutionsRoot)`
  /* SolutionThemes is rendered as a sibling of the section content with
     absolute inset:0 / z-index:0 so it sits behind every band. Section
     content lives in <Inner> at z-index:1 so it stays interactive and on
     top. */
  position: relative;
  isolation: isolate;
`;

const Inner = styled.div`
  position: relative;
  z-index: 1;
`;
