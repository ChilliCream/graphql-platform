"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { SolutionsRoot } from "../SolutionsRoot";
import { StarChart } from "./StarChart";

interface SolutionsCinematicRootProps {
  readonly slug: string;
  readonly children: ReactNode;
  readonly className?: string;
}

// SolutionsCinematicRoot wraps the standard SolutionsRoot and paints a
// per-slug astronomical star chart behind the section stack. The chart is
// the cinematic variant's signature: each solution slug renders a different
// constellation (Lyra, Hydra, Cygnus, etc.) so the page identity is
// telegraphed by the geometry of the lit stars on top of a shared field.
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
    <StarChart slug={slug} />
    <Inner>{children}</Inner>
  </Outer>
);

const Outer = styled(SolutionsRoot)`
  /* StarChart is rendered as a sibling of the section content with absolute
     inset:0 / z-index:0 so it tiles behind every band. Section content sits
     in <Inner> at z-index:1 so it stays interactive and on top. */
  position: relative;
  isolation: isolate;
`;

const Inner = styled.div`
  position: relative;
  z-index: 1;
`;
