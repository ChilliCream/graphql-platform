"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { TemplatesRoot } from "../TemplatesRoot";

import { BlueprintPaper } from "./BlueprintPaper";

// Cinematic shell for the /templates index. The cinematic variant inherits
// every token, typographic rule, and section CSS rule from the default
// `TemplatesRoot`, so the gallery, filter row, hero, and CTA strip render
// exactly as they do in the default variant. The single distinctive move is
// a `<BlueprintPaper>` backdrop layered behind the content, framing the page
// as drafting paper for "templates as engineering blueprints".
//
// The blueprint sits at z-index 0 and is pointer-events: none, so the normal
// content flow paints on top untouched. Section content inside the root must
// resolve at z-index >= 1 to win the stacking context; the inner styled
// override below lifts every direct `<section>` child accordingly.
const Shell = styled(TemplatesRoot)`
  & > section {
    position: relative;
    z-index: 1;
  }
`;

export interface TemplatesCinematicRootProps {
  readonly children?: ReactNode;
  readonly className?: string;
}

export const TemplatesCinematicRoot: FC<TemplatesCinematicRootProps> = ({
  children,
  className,
}) => {
  return (
    <Shell className={className}>
      <BlueprintPaper />
      {children}
    </Shell>
  );
};
