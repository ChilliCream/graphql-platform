"use client";

import React, { useEffect, useRef, useState } from "react";

interface ScaledCanvasProps {
  width: number;
  height: number;
  maxWidth?: number;
  padding?: number;
  children: React.ReactNode;
}

export const ScaledCanvas: React.FC<ScaledCanvasProps> = ({
  width,
  height,
  maxWidth = 1480,
  padding = 24,
  children,
}) => {
  const wrapRef = useRef<HTMLDivElement>(null);
  const [scale, setScale] = useState(1);

  useEffect(() => {
    const compute = () => {
      const node = wrapRef.current;
      const avail = node
        ? Math.min(node.clientWidth - padding * 2, maxWidth)
        : Math.min(window.innerWidth - padding * 2, maxWidth);
      const s = Math.min(1, avail / width);
      setScale(s > 0 ? s : 1);
    };
    compute();

    const node = wrapRef.current;
    if (node && typeof ResizeObserver !== "undefined") {
      const ro = new ResizeObserver(() => compute());
      ro.observe(node);
      window.addEventListener("resize", compute);
      return () => {
        ro.disconnect();
        window.removeEventListener("resize", compute);
      };
    }
    window.addEventListener("resize", compute);
    return () => window.removeEventListener("resize", compute);
  }, [width, padding, maxWidth]);

  return (
    <div
      className="cc-canvas-wrap"
      ref={wrapRef}
      style={{
        width: "100%",
        display: "flex",
        justifyContent: "center",
        padding: `0 ${padding}px`,
      }}
    >
      <div
        className="cc-canvas-scaler"
        style={{
          width: width * scale,
          height: height * scale,
          position: "relative",
          maxWidth: "100%",
        }}
      >
        <div
          className="cc-canvas"
          style={{
            width,
            height,
            position: "relative",
            transform: `scale(${scale})`,
            transformOrigin: "top left",
          }}
        >
          {children}
        </div>
      </div>
    </div>
  );
};
