"use client";

import React, { FC } from "react";

import type { Industry } from "@/data/customers/industries";

// Stylized industry tile with a stroke-rendered circle and an accent letter.
// Mirrors the EnterpriseHero monogram vocabulary so anonymous customer cards
// read as "the same family of evidence" as the enterprise trust strip.

interface AnonymousMonogramProps {
  readonly industry: Industry;
  readonly size?: number;
  readonly title?: string;
}

export const AnonymousMonogram: FC<AnonymousMonogramProps> = ({
  industry,
  size = 44,
  title,
}) => {
  const radius = size * 0.42;
  const centerY = size * 0.5;
  const fontSize = size * 0.5;
  return (
    <svg
      viewBox={`0 0 ${size} ${size}`}
      width={size}
      height={size}
      role="img"
      aria-label={title ?? `${industry.short} customer monogram`}
    >
      <g
        stroke={industry.accentVar}
        strokeWidth={Math.max(1.4, size * 0.04)}
        fill="none"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <circle cx={size / 2} cy={centerY} r={radius} opacity={0.7} />
      </g>
      <text
        x={size / 2}
        y={centerY + fontSize * 0.34}
        textAnchor="middle"
        fontFamily="var(--cc-font-sans), sans-serif"
        fontWeight={500}
        fontSize={fontSize}
        fill={industry.accentVar}
      >
        {industry.monogram}
      </text>
    </svg>
  );
};
