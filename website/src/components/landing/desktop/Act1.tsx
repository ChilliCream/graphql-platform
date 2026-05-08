"use client";

import React, { useLayoutEffect, useRef } from "react";

import { Cup } from "../Cup";
import { useAnchorContext } from "./AnchorContext";

const HERO_LANES = [22, 40, 58, 76];

const PRODUCTS = [
  { key: "hot-chocolate", label: "Hot Chocolate" },
  { key: "nitro", label: "Nitro" },
  { key: "mocha", label: "Mocha" },
  { key: "strawberry-shake", label: "Strawberry Shake" },
] as const;

const CUPS = [
  { ...PRODUCTS[0], cupX: 140, cupY: 120, tilt: 35, exitLane: HERO_LANES[0] },
  { ...PRODUCTS[1], cupX: 220, cupY: 460, tilt: 40, exitLane: HERO_LANES[1] },
  { ...PRODUCTS[2], cupX: 860, cupY: 90, tilt: -55, exitLane: HERO_LANES[2] },
  { ...PRODUCTS[3], cupX: 830, cupY: 470, tilt: -50, exitLane: HERO_LANES[3] },
] as const;

export const Act1: React.FC = () => {
  const cups = CUPS;

  const HERO_W = 1000;
  const HERO_H = 760;

  const sectionRef = useRef<HTMLElement>(null);
  const wrapRef = useRef<HTMLDivElement>(null);
  const { register, unregister } = useAnchorContext();

  useLayoutEffect(() => {
    const measure = () => {
      const wrap = wrapRef.current;
      const root = sectionRef.current?.closest(
        "[data-cc-landing-root]"
      ) as HTMLElement | null;
      if (!wrap || !root) return;
      const wRect = wrap.getBoundingClientRect();
      const rRect = root.getBoundingClientRect();
      // The hero canvas-wrap has aspect-ratio 1000/760 and centers the SVG
      // with preserveAspectRatio="xMidYMid meet" inside it. The SVG draws
      // in a 1000x760 viewBox that fills the wrap, so the scale matches.
      const scaleX = wRect.width / HERO_W;
      const scaleY = wRect.height / HERO_H;
      const toPage = (cx: number, cy: number) => ({
        x: wRect.left - rRect.left + cx * scaleX,
        y: wRect.top - rRect.top + cy * scaleY,
      });

      CUPS.forEach((c, i) => {
        const r = 24;
        const t = (c.tilt * Math.PI) / 180;
        const dx = Math.sin(t);
        const x0 = c.cupX + dx * r;
        const y0 = c.cupY + -Math.cos(t) * r;
        register(`act1.cup-${i}`, { ...toPage(x0, y0), kind: "cup-spout" });

        // Pre-compute elbow + hold geometry in canvas coords, scale down via
        // toPage so the connector can draw the pour as a sequence of anchor
        // points without re-deriving the scale.
        const lipOut = 40;
        const fall = 150;
        const tan1 = 80;
        const xElbow = x0 + dx * lipOut;
        const yElbow = y0 + fall;

        const xExit = (c.exitLane / 100) * HERO_W;
        const yExit = HERO_H;
        const verticalHoldMax = 280;
        const verticalHold = Math.min(verticalHoldMax, (yExit - yElbow) * 0.42);

        register(`act1.pour-elbow-${i}`, {
          ...toPage(xElbow, yElbow),
          kind: "pour-exit",
        });
        register(`act1.pour-elbow-tan-${i}`, {
          ...toPage(xElbow, yElbow - tan1),
          kind: "pour-exit",
        });
        register(`act1.pour-elbow-hold-${i}`, {
          ...toPage(xElbow, yElbow + verticalHold),
          kind: "pour-exit",
        });
        register(`act1.pour-exit-${i}`, {
          ...toPage(xExit, yExit),
          kind: "pour-exit",
        });
        register(`act1.pour-exit-pre-${i}`, {
          ...toPage(xExit, yExit - verticalHold),
          kind: "pour-exit",
        });
      });
    };

    measure();
    const ro = new ResizeObserver(measure);
    if (wrapRef.current) ro.observe(wrapRef.current);
    if (sectionRef.current) ro.observe(sectionRef.current);
    window.addEventListener("resize", measure);
    return () => {
      ro.disconnect();
      window.removeEventListener("resize", measure);
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

        {cups.map((c) => {
          const CUP_SVG = 64;
          // Place the label on the side OPPOSITE to where the cup tilts so it
          // doesn't sit on top of the pour. Positive tilt = cup leans right
          // (pour goes right), so label goes on the left, and vice versa.
          const labelOnRight = c.tilt < 0;
          return (
            <div
              key={"cup-" + c.key}
              className="cc-cup-pos-scatter"
              style={{
                left: `${(c.cupX / HERO_W) * 100}%`,
                top: `${(c.cupY / HERO_H) * 100}%`,
                width: `${(CUP_SVG / HERO_W) * 100}%`,
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
