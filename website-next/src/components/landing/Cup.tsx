"use client";

import React from "react";

interface CupProps {
  label?: string;
  tilt?: number;
  w?: number;
  scale?: number;
}

export const Cup: React.FC<CupProps> = ({ label, tilt = 45, w, scale = 1 }) => {
  const sized = w != null;
  const stroke = "var(--cc-ink)";
  const wrapStyle: React.CSSProperties = sized
    ? { width: w, height: w, position: "relative" }
    : { width: "100%", height: "100%", position: "relative" };
  const innerStyle: React.CSSProperties = {
    width: "100%",
    height: "100%",
    transform: `rotate(${tilt}deg) scale(${scale})`,
    transformOrigin: "center center",
  };

  return (
    <div className="cup-wrap" data-cup={label} style={wrapStyle}>
      <div style={innerStyle}>
        <svg
          viewBox="0 0 64 64"
          width="100%"
          height="100%"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
        >
          <path
            d="M19 18L22.5 54C22.7 56.2 24.5 58 26.7 58H37.3C39.5 58 41.3 56.2 41.5 54L45 18H19Z"
            fill={stroke}
            fillOpacity="0.1"
          />
          <path
            d="M19 18L22.5 54C22.7 56.2 24.5 58 26.7 58H37.3C39.5 58 41.3 56.2 41.5 54L45 18H19Z"
            stroke={stroke}
            strokeWidth="2.5"
            strokeLinejoin="round"
          />
          <path
            d="M16 14C16 12.3431 17.3431 11 19 11H45C46.6569 11 48 12.3431 48 14V17C48 18.1046 47.1046 19 46 19H18C16.8954 19 16 18.1046 16 17V14Z"
            fill={stroke}
          />
          <path
            d="M22 11V10C22 8.89543 22.8954 8 24 8H40C41.1046 8 42 8.89543 42 10V11"
            stroke={stroke}
            strokeWidth="2.5"
            strokeLinecap="round"
          />
          <path
            d="M19 14H45"
            stroke="#050514"
            strokeOpacity="0.2"
            strokeWidth="1"
          />
        </svg>
      </div>
      {label && (
        <div
          style={{
            position: "absolute",
            left: "calc(100% + 12px)",
            top: "50%",
            transform: "translateY(-50%)",
            fontFamily: "var(--cc-font-mono), monospace",
            fontSize: "11px",
            letterSpacing: "0.18em",
            textTransform: "uppercase",
            color: stroke,
            whiteSpace: "nowrap",
          }}
        >
          {label}
        </div>
      )}
    </div>
  );
};
