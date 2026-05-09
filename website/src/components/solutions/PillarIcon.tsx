"use client";

import React, { FC } from "react";

import type { IconKind } from "@/data/solutions/types";

// Stroke-icon vocabulary used by pillars and feature cards. Same drawing
// conventions as the brewer icons in EnterpriseSkuCards / Act5: stroke
// width 1.6, rounded caps, no fills, color = currentColor so the parent
// container controls the tint.
//
// Each kind maps to a single 24x24 glyph. Adding a new kind means
// extending IconKind and adding a path here; nothing else changes.

interface PillarIconProps {
  readonly kind: IconKind;
  readonly size?: number;
}

const PATHS: Record<IconKind, React.ReactNode> = {
  stack: (
    <>
      <path d="M12 4 L20 8 L12 12 L4 8 Z" />
      <path d="M4 12 L12 16 L20 12" />
      <path d="M4 16 L12 20 L20 16" />
    </>
  ),
  shield: (
    <>
      <path d="M12 3 L20 6 V12 C20 16 16 19 12 21 C8 19 4 16 4 12 V6 Z" />
      <path d="M9 12 L11 14 L15 10" />
    </>
  ),
  graph: (
    <>
      <circle cx="6" cy="7" r="2.2" />
      <circle cx="18" cy="7" r="2.2" />
      <circle cx="12" cy="17" r="2.2" />
      <line x1="7.7" y1="8.3" x2="10.7" y2="15.7" />
      <line x1="16.3" y1="8.3" x2="13.3" y2="15.7" />
      <line x1="8.2" y1="7" x2="15.8" y2="7" />
    </>
  ),
  bus: (
    <>
      <rect x="4" y="6" width="16" height="10" rx="2" />
      <line x1="8" y1="6" x2="8" y2="16" />
      <line x1="16" y1="6" x2="16" y2="16" />
      <circle cx="8" cy="19" r="1" />
      <circle cx="16" cy="19" r="1" />
    </>
  ),
  agent: (
    <>
      <rect x="5" y="8" width="14" height="11" rx="2" />
      <circle cx="9" cy="13" r="1.2" />
      <circle cx="15" cy="13" r="1.2" />
      <line x1="12" y1="4" x2="12" y2="8" />
      <circle cx="12" cy="3.5" r="1" />
      <line x1="3" y1="14" x2="5" y2="14" />
      <line x1="19" y1="14" x2="21" y2="14" />
    </>
  ),
  lock: (
    <>
      <rect x="5" y="11" width="14" height="9" rx="2" />
      <path d="M8 11 V8 a4 4 0 0 1 8 0 V11" />
      <line x1="12" y1="14" x2="12" y2="17" />
    </>
  ),
  scale: (
    <>
      <rect x="4" y="4" width="6" height="6" rx="1.4" />
      <rect x="14" y="4" width="6" height="6" rx="1.4" />
      <rect x="4" y="14" width="6" height="6" rx="1.4" />
      <rect x="14" y="14" width="6" height="6" rx="1.4" />
      <line x1="10" y1="7" x2="14" y2="7" />
      <line x1="10" y1="17" x2="14" y2="17" />
      <line x1="7" y1="10" x2="7" y2="14" />
      <line x1="17" y1="10" x2="17" y2="14" />
    </>
  ),
  audit: (
    <>
      <rect x="5" y="4" width="14" height="17" rx="2" />
      <line x1="8" y1="9" x2="16" y2="9" />
      <line x1="8" y1="13" x2="16" y2="13" />
      <line x1="8" y1="17" x2="13" y2="17" />
      <circle cx="17" cy="17" r="2.2" />
    </>
  ),
  speed: (
    <>
      <path d="M3 17 a9 9 0 0 1 18 0" />
      <line x1="12" y1="17" x2="16" y2="9" />
      <circle cx="12" cy="17" r="1.4" />
    </>
  ),
  globe: (
    <>
      <circle cx="12" cy="12" r="8.5" />
      <ellipse cx="12" cy="12" rx="3.5" ry="8.5" />
      <line x1="3.5" y1="12" x2="20.5" y2="12" />
    </>
  ),
  compose: (
    <>
      <circle cx="6" cy="6" r="2" />
      <circle cx="18" cy="6" r="2" />
      <circle cx="6" cy="18" r="2" />
      <circle cx="18" cy="18" r="2" />
      <circle cx="12" cy="12" r="2.6" />
      <line x1="7.4" y1="7.4" x2="10.2" y2="10.2" />
      <line x1="16.6" y1="7.4" x2="13.8" y2="10.2" />
      <line x1="7.4" y1="16.6" x2="10.2" y2="13.8" />
      <line x1="16.6" y1="16.6" x2="13.8" y2="13.8" />
    </>
  ),
  schema: (
    <>
      <rect x="4" y="4" width="16" height="4" rx="1" />
      <rect x="4" y="10" width="16" height="4" rx="1" />
      <rect x="4" y="16" width="16" height="4" rx="1" />
      <line x1="7" y1="6" x2="14" y2="6" opacity="0.5" />
      <line x1="7" y1="12" x2="14" y2="12" opacity="0.5" />
      <line x1="7" y1="18" x2="14" y2="18" opacity="0.5" />
    </>
  ),
};

export const PillarIcon: FC<PillarIconProps> = ({ kind, size = 22 }) => (
  <svg
    viewBox="0 0 24 24"
    width={size}
    height={size}
    aria-hidden
    fill="none"
    stroke="currentColor"
    strokeWidth={1.6}
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    {PATHS[kind]}
  </svg>
);
