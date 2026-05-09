"use client";

import React, { FC } from "react";

import type { SolutionRecord } from "@/data/solutions/types";

import { SolutionPageRenderer } from "../SolutionPageRenderer";

interface SolutionPageRendererCinematicProps {
  readonly record: SolutionRecord;
}

// Cinematic variant of /solutions/[slug]. The page composition is identical
// to the default `SolutionPageRenderer`; the cinematic distinction comes from
// the surrounding `<SolutionsCinematicRoot>` which paints a per-slug
// astronomical star chart behind the section stack. Keeping this renderer
// thin means the cinematic variant inherits every section update from the
// default automatically; the only cinematic-specific surface is the chart
// background.
export const SolutionPageRendererCinematic: FC<
  SolutionPageRendererCinematicProps
> = ({ record }) => <SolutionPageRenderer record={record} />;
