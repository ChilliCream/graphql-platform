"use client";

import React, { FC } from "react";

import {
  DashboardComposite,
  Hemisphere,
  LightCone,
  Orbit,
  PerspectiveGrid,
  RayBurst,
  TypographicMoment,
} from "@/components/redesign-system/illustrations";
import type { HeroMotif as HeroMotifKind } from "@/data/solutions/types";

interface HeroMotifProps {
  readonly kind: HeroMotifKind;
  readonly slug: string;
}

// Page-level hero illustration. Each solution slug picks a single motif so
// the hero canvas reads as solution-specific even though the layout is
// shared. Motifs are composed from the abstract-spatial illustration
// primitives plus the dashboard composite, all driven by the slug's accent
// thread (currentColor + var(--cc-accent)). Decorative.
export const HeroMotif: FC<HeroMotifProps> = ({ kind, slug }) => {
  switch (kind) {
    case "orbit": {
      // Event-driven uses a denser orbit (more rings, slightly rotated) so
      // the same primitive reads as "events flowing" instead of "satellites".
      const rings = slug === "event-driven" ? 5 : 3;
      const rotate = slug === "event-driven" ? -8 : 0;
      return <Orbit rings={rings} rotate={rotate} />;
    }
    case "hemisphere":
      return <Hemisphere side="right" />;
    case "perspective-grid":
      return <PerspectiveGrid density="dense" />;
    case "ray-burst":
      return <RayBurst rayCount={20} />;
    case "light-cone":
      return <LightCone angle={32} />;
    case "dashboard":
      return (
        <DashboardComposite
          panels={["chart", "trace"]}
          bleedDirection="right"
        />
      );
    case "typographic":
    default:
      return (
        <TypographicMoment
          text="ONE"
          unit="graph"
          variant="gradient"
          size="huge"
        />
      );
  }
};
