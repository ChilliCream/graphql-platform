"use client";

import React, { FC, useState, useCallback, useEffect } from "react";
import dynamic from "next/dynamic";
import "@xyflow/react/dist/style.css";

const TopologyVisualizationInner = dynamic(
  () =>
    import("./topology-visualization-inner").then(
      (m) => m.TopologyVisualizationInner
    ),
  { ssr: false }
);

const containerStyle: React.CSSProperties = {
  width: "100%",
  height: 500,
  borderRadius: 8,
  overflow: "hidden",
  border: "1px solid #30363d",
  margin: "24px 0",
  position: "relative",
};

const overlayStyle: React.CSSProperties = {
  position: "fixed",
  inset: 0,
  zIndex: 9999,
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  backgroundColor: "rgba(0, 0, 0, 0.6)",
  backdropFilter: "blur(4px)",
};

const popupStyle: React.CSSProperties = {
  width: "92vw",
  height: "88vh",
  borderRadius: 12,
  overflow: "hidden",
  border: "1px solid #30363d",
  backgroundColor: "#0d1117",
  position: "relative",
  boxShadow: "0 24px 48px rgba(0, 0, 0, 0.4)",
};

const iconButtonStyle: React.CSSProperties = {
  position: "absolute",
  top: 8,
  right: 8,
  zIndex: 10,
  width: 32,
  height: 32,
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  borderRadius: 6,
  border: "1px solid #30363d",
  backgroundColor: "#161b22",
  color: "#8b949e",
  cursor: "pointer",
  padding: 0,
  lineHeight: 1,
};

// Inline SVG icons — expand (arrows outward) and close (X)
const ExpandIcon = () => (
  <svg
    width="14"
    height="14"
    viewBox="0 0 14 14"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.5"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <polyline points="8.5,1 13,1 13,5.5" />
    <line x1="13" y1="1" x2="8" y2="6" />
    <polyline points="5.5,13 1,13 1,8.5" />
    <line x1="1" y1="13" x2="6" y2="8" />
  </svg>
);

const CloseIcon = () => (
  <svg
    width="14"
    height="14"
    viewBox="0 0 14 14"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.5"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <line x1="2" y1="2" x2="12" y2="12" />
    <line x1="12" y1="2" x2="2" y2="12" />
  </svg>
);

export interface TopologyVisualizationProps {
  readonly data?: string;
  readonly trace?: string;
}

export const TopologyVisualization: FC<TopologyVisualizationProps> = ({
  data,
  trace,
}) => {
  const [expanded, setExpanded] = useState(false);

  const open = useCallback(() => setExpanded(true), []);
  const close = useCallback(() => setExpanded(false), []);

  // Close on Escape
  useEffect(() => {
    if (!expanded) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "Escape") close();
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [expanded, close]);

  // Prevent body scroll when popup is open
  useEffect(() => {
    if (!expanded) return;
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = prev;
    };
  }, [expanded]);

  return (
    <>
      <div
        style={{ ...containerStyle, cursor: "pointer" }}
        onClick={open}
        title="Click to expand visualization"
      >
        <button
          style={iconButtonStyle}
          onClick={open}
          title="Expand visualization"
          aria-label="Expand visualization"
        >
          <ExpandIcon />
        </button>
        <div style={{ position: "absolute", inset: 0, zIndex: 5 }} />
        <TopologyVisualizationInner
          data={data}
          trace={trace}
          expanded={false}
        />
      </div>

      {expanded && (
        <div style={overlayStyle} onClick={close}>
          <div style={popupStyle} onClick={(e) => e.stopPropagation()}>
            <button
              style={iconButtonStyle}
              onClick={close}
              title="Close"
              aria-label="Close visualization"
            >
              <CloseIcon />
            </button>
            <TopologyVisualizationInner
              data={data}
              trace={trace}
              expanded={true}
            />
          </div>
        </div>
      )}
    </>
  );
};
