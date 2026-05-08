"use client";

import React from "react";

interface ActBundleProps {
  lanes?: 4 | 5;
}

export const ActBundle: React.FC<ActBundleProps> = ({ lanes = 4 }) => {
  const w = lanes === 5 ? 36 : 30;
  const cx = w / 2;
  const colors =
    lanes === 5
      ? [
          "var(--cc-col-cat)",
          "var(--cc-col-bil)",
          "var(--cc-col-ord)",
          "var(--cc-col-shi)",
          "var(--cc-col-usr)",
        ]
      : [
          "var(--cc-col-cat)",
          "var(--cc-col-bil)",
          "var(--cc-col-ord)",
          "var(--cc-col-shi)",
        ];
  const gap = 6;
  const totalWidth = (lanes - 1) * gap;
  const startX = cx - totalWidth / 2;
  const maskId = `cc-bundle-mask-${lanes}`;
  const fadeId = `cc-bundle-fade-${lanes}`;

  return (
    <svg
      className="act-bundle"
      width={w}
      height={48}
      viewBox={`0 0 ${w} 48`}
      aria-hidden
    >
      <defs>
        <linearGradient
          id={fadeId}
          x1="0"
          y1="0"
          x2="0"
          y2="48"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="black" />
          <stop offset="0.18" stopColor="white" />
          <stop offset="0.82" stopColor="white" />
          <stop offset="1" stopColor="black" />
        </linearGradient>
        <mask
          id={maskId}
          maskUnits="userSpaceOnUse"
          x="0"
          y="0"
          width={w}
          height="48"
        >
          <rect x="0" y="0" width="100%" height="48" fill={`url(#${fadeId})`} />
        </mask>
      </defs>
      <g mask={`url(#${maskId})`}>
        {colors.map((c, i) => (
          <line
            key={i}
            x1={startX + i * gap}
            y1="0"
            x2={startX + i * gap}
            y2="48"
            stroke={c}
            strokeWidth="2"
          />
        ))}
      </g>
    </svg>
  );
};
