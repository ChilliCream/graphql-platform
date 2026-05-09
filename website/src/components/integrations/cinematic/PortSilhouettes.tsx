"use client";

import React from "react";
import styled from "styled-components";

// Quiet ambient background for the cinematic /integrations variant. Two
// faint port-silhouette icons sit in opposite corners of the page: a
// stylized RJ45 socket in the top-right and a USB-A socket in the
// bottom-left. The composition reads as "endpoints / things plug in
// here" without filling the page with chrome. Pure stroke, hairline
// weights, no fills, no traces between, no repeating pattern.
//
// Each icon is its own small inline SVG positioned via CSS so the corner
// anchoring is exact regardless of viewport size. The wrapper sits at
// `position: absolute; inset: 0; z-index: 0;` behind the page content
// and is purely decorative.

export interface PortSilhouettesProps {
  className?: string;
}

const PORT_STROKE = "rgba(80, 200, 140, 0.10)";
const PORT_LABEL = "rgba(80, 200, 140, 0.18)";

const Outer = styled.div`
  position: absolute;
  inset: 0;
  z-index: 0;
  pointer-events: none;
  overflow: hidden;
`;

const TopRightPort = styled.div`
  position: absolute;
  top: 80px;
  right: 80px;
  width: 140px;
  height: 110px;
`;

const BottomLeftPort = styled.div`
  position: absolute;
  bottom: 120px;
  left: 80px;
  width: 120px;
  height: 80px;
`;

const PortSvg = styled.svg`
  display: block;
  width: 100%;
  height: 100%;
  overflow: visible;
`;

const PortLabel = styled.div`
  font-family: ui-monospace, SFMono-Regular, monospace;
  font-size: 8px;
  letter-spacing: 0.08em;
  color: ${PORT_LABEL};
  text-transform: uppercase;
`;

const TopRightLabel = styled(PortLabel)`
  position: absolute;
  top: 96px;
  text-align: right;
  width: 140px;
`;

const BottomLeftLabel = styled(PortLabel)`
  position: absolute;
  top: 68px;
  left: 0;
`;

/**
 * Decorative corner-anchored port silhouettes for the cinematic
 * /integrations variant. Renders an RJ45 outline in the top-right and a
 * USB-A outline in the bottom-left, both at low opacity. Hidden from
 * assistive tech and never receives pointer events.
 */
export const PortSilhouettes: React.FC<PortSilhouettesProps> = ({
  className,
}) => {
  return (
    <Outer className={className} aria-hidden="true">
      {/* Top-right: stylized RJ45 socket. The outer trapezoid is the
          plug-receiving cavity; the inner notch suggests the locking
          tab cutout; eight short verticals form the contact strip. */}
      <TopRightPort>
        <PortSvg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 140 90"
          fill="none"
          stroke={PORT_STROKE}
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          {/* Outer port body */}
          <path d="M 10 14 L 130 14 L 130 76 L 10 76 Z" />
          {/* Plug cavity (inner trapezoid mimicking the RJ45 keystone) */}
          <path d="M 22 22 L 118 22 L 118 60 L 96 60 L 96 68 L 44 68 L 44 60 L 22 60 Z" />
          {/* 8-pin contact strip */}
          <path d="M 50 30 L 50 44" />
          <path d="M 56 30 L 56 44" />
          <path d="M 62 30 L 62 44" />
          <path d="M 68 30 L 68 44" />
          <path d="M 74 30 L 74 44" />
          <path d="M 80 30 L 80 44" />
          <path d="M 86 30 L 86 44" />
          <path d="M 92 30 L 92 44" />
        </PortSvg>
        <TopRightLabel>RJ45-T568B</TopRightLabel>
      </TopRightPort>

      {/* Bottom-left: stylized USB-A socket. Outer rectangle is the
          shielded shell; inner rectangle is the plastic tongue with
          the four contacts. */}
      <BottomLeftPort>
        <PortSvg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 120 60"
          fill="none"
          stroke={PORT_STROKE}
          strokeWidth={1.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          {/* Outer shielded shell */}
          <path d="M 6 10 L 114 10 L 114 50 L 6 50 Z" />
          {/* Plastic tongue (offset to the top edge as on a real USB-A) */}
          <path d="M 18 18 L 102 18 L 102 32 L 18 32 Z" />
          {/* 4 contact pads on the tongue */}
          <path d="M 30 22 L 42 22" />
          <path d="M 52 22 L 64 22" />
          <path d="M 74 22 L 86 22" />
          <path d="M 94 22 L 100 22" />
        </PortSvg>
        <BottomLeftLabel>USB-A</BottomLeftLabel>
      </BottomLeftPort>
    </Outer>
  );
};
