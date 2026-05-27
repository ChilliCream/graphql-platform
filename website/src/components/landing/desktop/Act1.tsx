"use client";

import React, { useLayoutEffect, useRef } from "react";

import { Cup } from "../Cup";
import { useAnchorContext } from "./AnchorContext";

// Hero layout — coordinates expressed as fractions of the hero wrap so the
// layout flows naturally with viewport width. The original design (which the
// connector geometry was tuned against) was authored in a 1000×760 coord
// space; we keep that reference space alive only to express percentages.
const HERO_REF_W = 1000;
const HERO_REF_H = 760;

const PRODUCTS = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "nitro", label: "Nitro" },
  { key: "mocha", label: "Mocha" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
] as const;

// Cup configuration. `cupX/cupY` are positions in the 1000×760 reference
// space (top-left of the cup); they are converted to percentages of the wrap
// on render. `exitLanePct` is the horizontal exit lane the pour heads toward,
// expressed as a fraction of the wrap width.
const CUPS = [
  {
    ...PRODUCTS[0],
    cupX: 140,
    cupY: 120,
    tilt: 35,
    exitLanePct: 0.22,
  },
  {
    ...PRODUCTS[1],
    cupX: 220,
    cupY: 460,
    tilt: 40,
    exitLanePct: 0.4,
  },
  {
    ...PRODUCTS[2],
    cupX: 860,
    cupY: 90,
    tilt: -55,
    exitLanePct: 0.58,
  },
  {
    ...PRODUCTS[3],
    cupX: 830,
    cupY: 470,
    tilt: -50,
    exitLanePct: 0.76,
  },
] as const;

// Cup SVG bounding-box size in the reference space (the Cup component renders
// into a 64×64 viewBox).
const CUP_REF_SIZE = 64;

export const Act1: React.FC = () => {
  const sectionRef = useRef<HTMLElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { register, unregister } = useAnchorContext();

  // Cup anchors are derived geometrically from the cup wrap's measured rect
  // plus the cup's tilt. We don't use useMeasuredAnchor here because every
  // anchor (spout + 5 pour control points) is a derived position computed
  // off the same wrap rect, so a single measure callback writes them all in
  // one pass.
  useLayoutEffect(() => {
    const measure = () => {
      const wrap = wrapRef.current;
      const root = sectionRef.current?.closest(
        "[data-cc-landing-root]"
      ) as HTMLElement | null;
      if (!wrap || !root) {
        return;
      }
      const wRect = wrap.getBoundingClientRect();
      const rRect = root.getBoundingClientRect();
      const wrapW = wRect.width;
      const wrapH = wRect.height;
      // Convert wrap-local (in reference units) to landing-root pixel coords.
      const project = (refX: number, refY: number) => ({
        x: wRect.left - rRect.left + (refX / HERO_REF_W) * wrapW,
        y: wRect.top - rRect.top + (refY / HERO_REF_H) * wrapH,
      });

      CUPS.forEach((c, i) => {
        // Spout offset from cup center, tilt-derived. `(cupX, cupY)` is the
        // cup's visual center (the `.cc-cup-pos-scatter` div has
        // `transform: translate(-50%, -50%)`), and the spout sits a fixed
        // radius `r` from the center in the tilt direction.
        const r = 24;
        const t = (c.tilt * Math.PI) / 180;
        const dxUnit = Math.sin(t);
        const dyUnit = -Math.cos(t);
        const xSpout = c.cupX + dxUnit * r;
        const ySpout = c.cupY + dyUnit * r;
        register(`act1.cup-${i}`, {
          ...project(xSpout, ySpout),
          kind: "cup-spout",
        });

        // Bezier control points along the pour. Offsets are in reference
        // units so they scale 1:1 with the wrap.
        const lipOut = 40;
        const fall = 150;
        const tan1 = 80;
        const xElbow = xSpout + dxUnit * lipOut;
        const yElbow = ySpout + fall;

        const xExit = c.exitLanePct * HERO_REF_W;
        const yExit = HERO_REF_H;
        const verticalHoldMax = 280;
        const verticalHold = Math.min(verticalHoldMax, (yExit - yElbow) * 0.42);

        register(`act1.pour-elbow-${i}`, {
          ...project(xElbow, yElbow),
          kind: "pour-exit",
        });
        register(`act1.pour-elbow-tan-${i}`, {
          ...project(xElbow, yElbow - tan1),
          kind: "pour-exit",
        });
        register(`act1.pour-elbow-hold-${i}`, {
          ...project(xElbow, yElbow + verticalHold),
          kind: "pour-exit",
        });
        register(`act1.pour-exit-${i}`, {
          ...project(xExit, yExit),
          kind: "pour-exit",
        });
        register(`act1.pour-exit-pre-${i}`, {
          ...project(xExit, yExit - verticalHold),
          kind: "pour-exit",
        });
      });
    };

    measure();
    const ro = new ResizeObserver(measure);
    if (wrapRef.current) {
      ro.observe(wrapRef.current);
    }
    if (sectionRef.current) {
      ro.observe(sectionRef.current);
    }
    const scrollEl = document.querySelector(
      ".main__Container-sc-d4365469-0"
    ) as HTMLElement | null;
    scrollEl?.addEventListener("scroll", measure, { passive: true });
    window.addEventListener("scroll", measure, { passive: true });
    window.addEventListener("resize", measure);
    const fontShiftTimer = window.setTimeout(measure, 250);
    return () => {
      ro.disconnect();
      scrollEl?.removeEventListener("scroll", measure);
      window.removeEventListener("scroll", measure);
      window.removeEventListener("resize", measure);
      window.clearTimeout(fontShiftTimer);
      CUPS.forEach((_, i) => {
        unregister(`act1.cup-${i}`);
        unregister(`act1.pour-elbow-${i}`);
        unregister(`act1.pour-elbow-tan-${i}`);
        unregister(`act1.pour-elbow-hold-${i}`);
        unregister(`act1.pour-exit-${i}`);
        unregister(`act1.pour-exit-pre-${i}`);
      });
    };
  }, [register, unregister]);

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-hero"
      data-screen-label="01 Hero"
    >
      <div className="cc-act-label">
        <span className="num">01</span> Hero
      </div>

      <div className="cc-hero-canvas-wrap" ref={wrapRef}>
        <div className="cc-hero-copy">
          <div className="eyebrow">The API platform</div>
          <h1 className="display">
            The API Platform
            <br />
            <span className="accent">for Humans and Agents</span>
          </h1>
          <p>
            Unify all your APIs into a comprehensive company graph, streamlining
            data accessibility and enhancing integration. Transform the way you
            manage and interact with your data.
          </p>
          <div className="cc-cta-row">
            <button className="cc-btn cc-btn-primary">Start pouring →</button>
            <button className="cc-btn cc-btn-ghost">Read the docs</button>
          </div>
        </div>

        {CUPS.map((c) => {
          // Place the label on the side OPPOSITE to where the cup tilts so it
          // doesn't sit on top of the pour. Positive tilt = cup leans right
          // (pour goes right), so label goes on the left, and vice versa.
          const labelOnRight = c.tilt < 0;
          return (
            <div
              key={"cup-" + c.key}
              className="cc-cup-pos-scatter"
              style={{
                left: `${(c.cupX / HERO_REF_W) * 100}%`,
                top: `${(c.cupY / HERO_REF_H) * 100}%`,
                width: `${(CUP_REF_SIZE / HERO_REF_W) * 100}%`,
                aspectRatio: "1",
              }}
            >
              <Cup tilt={c.tilt} />
              <div
                className="cc-cup-label"
                style={{
                  position: "absolute",
                  top: "50%",
                  ...(labelOnRight
                    ? { left: "calc(100% + 14px)" }
                    : { right: "calc(100% + 14px)" }),
                  transform: "translateY(-50%)",
                  fontFamily: "var(--cc-font-mono), monospace",
                  fontSize: "13px",
                  letterSpacing: "0.18em",
                  textTransform: "uppercase",
                  color: "var(--cc-ink)",
                  whiteSpace: "nowrap",
                }}
              >
                {c.label}
              </div>
            </div>
          );
        })}
      </div>
    </section>
  );
};
