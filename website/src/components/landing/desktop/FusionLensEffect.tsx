"use client";

import React, { useEffect, useRef, useState } from "react";

import { useAnchorContext } from "./AnchorContext";

// FusionLensEffect — lens overlay rendered at Act 3's pinch point. Uses
// the `subtle` preset with ambient glow disabled (see CONFIG below).

// =====================================================================
// Types & defaults
// =====================================================================

interface LensConfig {
  layers: {
    ambient: boolean;
    halo: boolean;
    core: boolean;
    glare: boolean;
    streamers: boolean;
    cross: boolean;
    xFlare: boolean;
    starburst: boolean;
    sparkles: boolean;
    rings: boolean;
    colorTint: boolean;
  };
  glareWidth: number;
  intensityMul: number;
}

const PRESETS: Record<string, LensConfig> = {
  off: {
    layers: {
      ambient: false,
      halo: false,
      core: false,
      glare: false,
      streamers: false,
      cross: false,
      xFlare: false,
      starburst: false,
      sparkles: false,
      rings: false,
      colorTint: false,
    },
    glareWidth: 200,
    intensityMul: 1,
  },
  subtle: {
    layers: {
      ambient: true,
      halo: true,
      core: true,
      glare: true,
      streamers: true,
      cross: true,
      xFlare: false,
      starburst: false,
      sparkles: false,
      rings: false,
      colorTint: false,
    },
    glareWidth: 200,
    intensityMul: 1,
  },
  punch: {
    layers: {
      ambient: true,
      halo: true,
      core: true,
      glare: true,
      streamers: true,
      cross: true,
      xFlare: true,
      starburst: false,
      sparkles: false,
      rings: false,
      colorTint: false,
    },
    glareWidth: 200,
    intensityMul: 1.7,
  },
  wide: {
    layers: {
      ambient: true,
      halo: true,
      core: true,
      glare: true,
      streamers: true,
      cross: true,
      xFlare: false,
      starburst: false,
      sparkles: false,
      rings: false,
      colorTint: false,
    },
    glareWidth: 520,
    intensityMul: 1,
  },
  massive: {
    layers: {
      ambient: true,
      halo: true,
      core: true,
      glare: true,
      streamers: true,
      cross: true,
      xFlare: true,
      starburst: false,
      sparkles: true,
      rings: false,
      colorTint: false,
    },
    glareWidth: 1600,
    intensityMul: 1.4,
  },
  everything: {
    layers: {
      ambient: true,
      halo: true,
      core: true,
      glare: true,
      streamers: true,
      cross: true,
      xFlare: true,
      starburst: true,
      sparkles: true,
      rings: true,
      colorTint: true,
    },
    glareWidth: 400,
    intensityMul: 1.2,
  },
};

// Active lens config: the `subtle` preset with ambient glow disabled.
const CONFIG: LensConfig = {
  ...PRESETS.subtle,
  layers: { ...PRESETS.subtle.layers, ambient: false },
};

const STREAMER_PERIOD = 80;

// =====================================================================
// Keyframes
// =====================================================================

const KEYFRAMES = `
@keyframes cc-lens-breathe {
  0%, 100% { transform: scale(1);    opacity: 1; }
  50%      { transform: scale(1.04); opacity: 0.92; }
}
@keyframes cc-lens-glare-shimmer {
  0%, 100% { opacity: 1;    transform: scaleX(1); }
  50%      { opacity: 0.85; transform: scaleX(1.02); }
}
@keyframes cc-lens-edge-drift {
  0%, 100% { opacity: 1;    transform: scale(1); }
  50%      { opacity: 0.88; transform: scale(1.02); }
}
@keyframes cc-lens-streamer-flow-fwd {
  from { transform: translateX(0); }
  to   { transform: translateX(-${STREAMER_PERIOD}px); }
}
@keyframes cc-lens-streamer-flow-rev {
  from { transform: translateX(0); }
  to   { transform: translateX(${STREAMER_PERIOD}px); }
}
@keyframes cc-lens-starburst-spin {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}
@keyframes cc-lens-color-tint-spin {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}
@keyframes cc-lens-ring-pulse {
  0%   { transform: scale(0.4); opacity: 0; }
  20%  { opacity: 0.9; }
  100% { transform: scale(2.6); opacity: 0; }
}
@keyframes cc-lens-sparkle-twinkle {
  0%, 100% { opacity: 0; transform: scale(0.4); }
  50%      { opacity: 1; transform: scale(1); }
}
`;

// =====================================================================
// Streamers (animated wavy lines through the glare)
// =====================================================================

interface Streamer {
  amp: number;
  phaseDeg: number;
  width: number;
  opacityMul: number;
  dur: number;
  reverse?: boolean;
}

const STREAMERS: Streamer[] = [
  { amp: 2.5, phaseDeg: 0, width: 0.6, opacityMul: 0.55, dur: 2.8 },
  {
    amp: 4,
    phaseDeg: 72,
    width: 0.5,
    opacityMul: 0.45,
    dur: 3.6,
    reverse: true,
  },
  { amp: 6, phaseDeg: 144, width: 0.5, opacityMul: 0.38, dur: 2.4 },
  {
    amp: 3.5,
    phaseDeg: 216,
    width: 0.6,
    opacityMul: 0.5,
    dur: 4.2,
    reverse: true,
  },
  { amp: 5.5, phaseDeg: 288, width: 0.5, opacityMul: 0.4, dur: 3.1 },
];

function makeWavePath(
  width: number,
  midY: number,
  amp: number,
  period: number,
  phaseDeg: number
): string {
  const phase = (phaseDeg * Math.PI) / 180;
  const step = 3;
  const startX = -period;
  const endX = width + period;
  const parts: string[] = [];
  for (let x = startX; x <= endX; x += step) {
    const y = midY + amp * Math.sin((2 * Math.PI * x) / period + phase);
    parts.push(`${x.toFixed(1)} ${y.toFixed(1)}`);
  }
  return "M " + parts.join(" L ");
}

// =====================================================================
// Sparkles — fixed pseudo-random positions so they don't shimmer
// position on every render
// =====================================================================

interface Sparkle {
  dx: number; // offset from pinch (px)
  dy: number;
  size: number; // diameter
  delay: number; // animation delay seconds
  dur: number; // animation duration seconds
}

function makeSparkles(glareWidth: number, count = 18): Sparkle[] {
  const out: Sparkle[] = [];
  // Deterministic pseudo-random so the sparkles don't reshuffle every
  // render — keyed off a fixed seed.
  let seed = 12345;
  const rand = () => {
    seed = (seed * 9301 + 49297) % 233280;
    return seed / 233280;
  };
  for (let i = 0; i < count; i++) {
    out.push({
      dx: (rand() - 0.5) * glareWidth * 0.9,
      dy: (rand() - 0.5) * 32,
      size: 1.5 + rand() * 2.5,
      delay: rand() * 4,
      dur: 1.6 + rand() * 2.4,
    });
  }
  return out;
}

// =====================================================================
// Pinch-tracking state (driven by scroll position)
// =====================================================================

interface State {
  pinchScreenX: number;
  pinchScreenY: number;
  intensity: number;
  vw: number;
  vh: number;
  visible: boolean;
}

const INITIAL: State = {
  pinchScreenX: 0,
  pinchScreenY: 0,
  intensity: 0,
  vw: 0,
  vh: 0,
  visible: false,
};

const smoothstep = (t: number) => t * t * (3 - 2 * t);

// =====================================================================
// Component
// =====================================================================

export const FusionLensEffect: React.FC = () => {
  const { anchors } = useAnchorContext();
  const [state, setState] = useState<State>(INITIAL);
  const config = CONFIG;
  const rafRef = useRef<number | null>(null);

  useEffect(() => {
    const compute = () => {
      const pinchAnchor = anchors["act3.pinch"];
      const root = document.querySelector(
        "[data-cc-landing-root]"
      ) as HTMLElement | null;
      if (!pinchAnchor || !root) {
        setState((s) =>
          s.visible ? { ...s, visible: false, intensity: 0 } : s
        );
        return;
      }
      const rRect = root.getBoundingClientRect();
      const screenX = rRect.left + pinchAnchor.x;
      const screenY = rRect.top + pinchAnchor.y;
      const vw = window.innerWidth;
      const vh = window.innerHeight;

      let i: number;
      if (screenY > vh) {
        const range = vh * 0.6;
        i = Math.max(0, 1 - (screenY - vh) / range);
      } else if (screenY < 0) {
        const range = vh * 0.6;
        i = Math.max(0, 1 - -screenY / range);
      } else {
        i = 1;
      }
      i = smoothstep(Math.min(1, Math.max(0, i)));

      setState({
        pinchScreenX: screenX,
        pinchScreenY: screenY,
        intensity: i,
        vw,
        vh,
        visible: i > 0.005,
      });
    };

    const schedule = () => {
      if (rafRef.current != null) return;
      rafRef.current = requestAnimationFrame(() => {
        rafRef.current = null;
        compute();
      });
    };

    compute();
    const scrollEl = document.querySelector(
      ".main__Container-sc-d4365469-0"
    ) as HTMLElement | null;
    scrollEl?.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("scroll", schedule, { passive: true });
    window.addEventListener("resize", schedule);

    return () => {
      scrollEl?.removeEventListener("scroll", schedule);
      window.removeEventListener("scroll", schedule);
      window.removeEventListener("resize", schedule);
      if (rafRef.current != null) cancelAnimationFrame(rafRef.current);
    };
  }, [anchors]);

  if (!state.visible) {
    return null;
  }

  const { pinchScreenX, pinchScreenY, intensity, vw, vh } = state;
  const inView = pinchScreenY >= 0 && pinchScreenY <= vh;
  const clampedY = Math.max(0, Math.min(vh, pinchScreenY));
  const focus = inView ? 1 : 0;
  const I = focus * intensity * config.intensityMul;
  const { glareWidth } = config;
  const L = config.layers;

  return (
    <div
      aria-hidden
      style={{
        position: "fixed",
        inset: 0,
        pointerEvents: "none",
        zIndex: 1,
        overflow: "hidden",
      }}
    >
      <style dangerouslySetInnerHTML={{ __html: KEYFRAMES }} />

      {/* Stage 1: ambient edge glow */}
      {L.ambient && (
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: `radial-gradient(ellipse 60vw 35vh at ${pinchScreenX}px ${clampedY}px,
                transparent 0%,
                rgba(255, 220, 180, ${
                  intensity * 0.05 * config.intensityMul
                }) 55%,
                rgba(220, 200, 255, ${
                  intensity * 0.07 * config.intensityMul
                }) 100%
              )`,
            mixBlendMode: "screen",
            animation: "cc-lens-edge-drift 9s ease-in-out infinite",
            willChange: "transform, opacity",
          }}
        />
      )}

      {inView && (
        <>
          {/* Color tint — conic rainbow behind everything */}
          {L.colorTint && (
            <div
              style={{
                position: "absolute",
                left: pinchScreenX - 60,
                top: clampedY - 60,
                width: 120,
                height: 120,
                borderRadius: "50%",
                background: `conic-gradient(from 0deg at 50% 50%,
                    var(--cc-col-cat),
                    var(--cc-col-bil),
                    var(--cc-col-ord),
                    var(--cc-col-shi),
                    var(--cc-col-usr),
                    var(--cc-col-cat)
                  )`,
                filter: "blur(18px)",
                mixBlendMode: "screen",
                opacity: I * 0.45,
                animation: "cc-lens-color-tint-spin 18s linear infinite",
                transformOrigin: "50% 50%",
                willChange: "transform, opacity",
              }}
            />
          )}

          {/* Pulse rings — concentric expanding circles */}
          {L.rings && (
            <>
              {[0, 1, 2].map((i) => (
                <div
                  key={i}
                  style={{
                    position: "absolute",
                    left: pinchScreenX - 30,
                    top: clampedY - 30,
                    width: 60,
                    height: 60,
                    borderRadius: "50%",
                    border: `1px solid rgba(255, 240, 210, ${I * 0.55})`,
                    mixBlendMode: "screen",
                    animation: `cc-lens-ring-pulse 3.6s ease-out infinite`,
                    animationDelay: `${i * 1.2}s`,
                    transformOrigin: "50% 50%",
                    willChange: "transform, opacity",
                  }}
                />
              ))}
            </>
          )}

          {/* Starburst — 8 thin radial spikes */}
          {L.starburst && (
            <div
              style={{
                position: "absolute",
                left: pinchScreenX - glareWidth * 0.25,
                top: clampedY - glareWidth * 0.25,
                width: glareWidth * 0.5,
                height: glareWidth * 0.5,
                pointerEvents: "none",
                animation: "cc-lens-starburst-spin 24s linear infinite",
                transformOrigin: "50% 50%",
                willChange: "transform",
              }}
            >
              {[0, 22.5, 45, 67.5, 90, 112.5, 135, 157.5].map((deg) => (
                <div
                  key={deg}
                  style={{
                    position: "absolute",
                    left: "50%",
                    top: 0,
                    width: 1,
                    height: "50%",
                    transform: `translateX(-50%) rotate(${deg}deg)`,
                    transformOrigin: "50% 100%",
                    background: `linear-gradient(180deg,
                        transparent 0%,
                        rgba(255, 245, 220, ${I * 0.5}) 45%,
                        rgba(255, 252, 240, ${I * 0.85}) 100%
                      )`,
                    filter: "blur(0.5px)",
                    mixBlendMode: "screen",
                  }}
                />
              ))}
            </div>
          )}

          {/* Horizontal glare */}
          {L.glare && glareWidth > 0 && (
            <>
              <div
                style={{
                  position: "absolute",
                  left: pinchScreenX - glareWidth / 2,
                  top: clampedY - 12,
                  width: glareWidth,
                  height: 24,
                  background: `linear-gradient(90deg,
                      transparent 0%,
                      rgba(255, 230, 195, ${I * 0.12}) 22%,
                      rgba(255, 245, 220, ${I * 0.3}) 38%,
                      rgba(255, 252, 235, ${I * 0.42}) 50%,
                      rgba(255, 245, 220, ${I * 0.34}) 60%,
                      rgba(255, 235, 200, ${I * 0.24}) 72%,
                      rgba(255, 225, 180, ${I * 0.13}) 86%,
                      transparent 100%
                    )`,
                  filter: "blur(5px)",
                  mixBlendMode: "screen",
                  animation: "cc-lens-glare-shimmer 3.6s ease-in-out infinite",
                  transformOrigin: "50% 50%",
                  willChange: "transform, opacity",
                }}
              />
              <div
                style={{
                  position: "absolute",
                  left: pinchScreenX - glareWidth / 2,
                  top: clampedY - 1,
                  width: glareWidth,
                  height: 2,
                  background: `linear-gradient(90deg,
                      transparent 0%,
                      rgba(255, 245, 220, ${I * 0.22}) 32%,
                      rgba(255, 252, 240, ${I * 0.55}) 50%,
                      rgba(255, 248, 230, ${I * 0.4}) 62%,
                      rgba(255, 240, 210, ${I * 0.22}) 78%,
                      transparent 100%
                    )`,
                  filter: "blur(0.8px)",
                  mixBlendMode: "screen",
                  animation:
                    "cc-lens-glare-shimmer 2.8s ease-in-out infinite reverse",
                  willChange: "transform, opacity",
                }}
              />
            </>
          )}

          {/* Streamers */}
          {L.streamers && glareWidth > 0 && (
            <div
              style={{
                position: "absolute",
                left: pinchScreenX - glareWidth / 2,
                top: clampedY - 12,
                width: glareWidth,
                height: 24,
                pointerEvents: "none",
                maskImage:
                  "radial-gradient(ellipse 60% 80% at 55% 50%, rgba(0,0,0,1) 12%, rgba(0,0,0,0) 95%)",
                WebkitMaskImage:
                  "radial-gradient(ellipse 60% 80% at 55% 50%, rgba(0,0,0,1) 12%, rgba(0,0,0,0) 95%)",
                mixBlendMode: "screen",
                opacity: I,
              }}
            >
              <svg
                width="100%"
                height="100%"
                viewBox={`0 0 ${glareWidth} 24`}
                preserveAspectRatio="none"
                style={{ overflow: "visible" }}
              >
                {STREAMERS.map((s, i) => (
                  <path
                    key={i}
                    d={makeWavePath(
                      glareWidth,
                      12,
                      s.amp * 0.75,
                      STREAMER_PERIOD,
                      s.phaseDeg
                    )}
                    stroke={`rgba(255, 255, 255, ${s.opacityMul})`}
                    strokeWidth={s.width}
                    fill="none"
                    strokeLinecap="round"
                    vectorEffect="non-scaling-stroke"
                    style={{
                      animation: `${
                        s.reverse
                          ? "cc-lens-streamer-flow-rev"
                          : "cc-lens-streamer-flow-fwd"
                      } ${s.dur}s linear infinite`,
                      willChange: "transform",
                    }}
                  />
                ))}
              </svg>
            </div>
          )}

          {/* Vertical cross-flare spike */}
          {L.cross && glareWidth > 0 && (
            <div
              style={{
                position: "absolute",
                left: pinchScreenX - 0.75,
                top: clampedY - glareWidth * 0.25,
                width: 1.5,
                height: glareWidth * 0.5,
                background: `linear-gradient(180deg,
                    transparent 0%,
                    rgba(255, 245, 220, ${I * 0.2}) 35%,
                    rgba(255, 252, 240, ${I * 0.5}) 50%,
                    rgba(255, 245, 220, ${I * 0.2}) 65%,
                    transparent 100%
                  )`,
                filter: "blur(0.6px)",
                mixBlendMode: "screen",
                animation:
                  "cc-lens-glare-shimmer 3.4s ease-in-out infinite reverse",
                willChange: "transform, opacity",
              }}
            />
          )}

          {/* Diagonal X-flare */}
          {L.xFlare && glareWidth > 0 && (
            <>
              {[45, -45].map((deg) => (
                <div
                  key={deg}
                  style={{
                    position: "absolute",
                    left: pinchScreenX - 0.75,
                    top: clampedY - glareWidth * 0.2,
                    width: 1.5,
                    height: glareWidth * 0.4,
                    transform: `rotate(${deg}deg)`,
                    transformOrigin: "50% 50%",
                    background: `linear-gradient(180deg,
                        transparent 0%,
                        rgba(255, 245, 220, ${I * 0.15}) 35%,
                        rgba(255, 252, 240, ${I * 0.4}) 50%,
                        rgba(255, 245, 220, ${I * 0.15}) 65%,
                        transparent 100%
                      )`,
                    filter: "blur(0.7px)",
                    mixBlendMode: "screen",
                    willChange: "transform, opacity",
                  }}
                />
              ))}
            </>
          )}

          {/* Sparkles */}
          {L.sparkles && glareWidth > 0 && (
            <>
              {makeSparkles(glareWidth).map((s, i) => (
                <div
                  key={i}
                  style={{
                    position: "absolute",
                    left: pinchScreenX + s.dx - s.size / 2,
                    top: clampedY + s.dy - s.size / 2,
                    width: s.size,
                    height: s.size,
                    borderRadius: "50%",
                    background: `rgba(255, 250, 235, ${I * 0.85})`,
                    boxShadow: `0 0 ${s.size * 2}px rgba(255, 240, 210, ${
                      I * 0.6
                    })`,
                    mixBlendMode: "screen",
                    animation: `cc-lens-sparkle-twinkle ${s.dur}s ease-in-out infinite`,
                    animationDelay: `${s.delay}s`,
                    willChange: "transform, opacity",
                  }}
                />
              ))}
            </>
          )}

          {/* Halo */}
          {L.halo && (
            <div
              style={{
                position: "absolute",
                left: pinchScreenX - 50,
                top: clampedY - 18,
                width: 100,
                height: 36,
                background: `radial-gradient(ellipse 50% 50% at 50% 50%,
                    rgba(255, 245, 220, ${I * 0.35}) 0%,
                    rgba(255, 220, 180, ${I * 0.15}) 50%,
                    transparent 100%
                  )`,
                filter: "blur(7px)",
                mixBlendMode: "screen",
                animation: "cc-lens-breathe 4.2s ease-in-out infinite",
                transformOrigin: "50% 50%",
                willChange: "transform, opacity",
              }}
            />
          )}

          {/* Core */}
          {L.core && (
            <div
              style={{
                position: "absolute",
                left: pinchScreenX - 11,
                top: clampedY - 11,
                width: 22,
                height: 22,
                borderRadius: "50%",
                background: `radial-gradient(circle at 50% 50%,
                    rgba(255, 255, 250, ${I * 0.65}) 0%,
                    rgba(255, 245, 220, ${I * 0.3}) 50%,
                    transparent 100%
                  )`,
                filter: "blur(2px)",
                mixBlendMode: "screen",
                animation: "cc-lens-breathe 2.6s ease-in-out infinite",
                transformOrigin: "50% 50%",
                willChange: "transform, opacity",
              }}
            />
          )}
        </>
      )}
      {/* unused vars */}
      <span style={{ display: "none" }}>{vw}</span>
    </div>
  );
};
