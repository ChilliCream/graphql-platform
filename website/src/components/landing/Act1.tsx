"use client";

import React from "react";

import { Cup } from "./Cup";
import { SHARED_PRODUCTS } from "./LandingRoot";

interface CupSpec {
  key: string;
  label: string;
  x: number;
  y: number;
  tilt: number;
  lane: number;
}

export const Act1: React.FC = () => {
  const HERO_W = 360;
  const HERO_H = 640;

  const cups: CupSpec[] = [
    { ...SHARED_PRODUCTS[0], x: 30, y: HERO_H * 0.07, tilt: 32, lane: 1 },
    { ...SHARED_PRODUCTS[1], x: 322, y: HERO_H * 0.16, tilt: -48, lane: 2 },
    { ...SHARED_PRODUCTS[2], x: 48, y: HERO_H * 0.84, tilt: 52, lane: 0 },
    { ...SHARED_PRODUCTS[3], x: 308, y: HERO_H * 0.72, tilt: -58, lane: 3 },
  ];

  const LANES_X = [171, 177, 183, 189];

  const pourPath = (cup: CupSpec) => {
    const r = 18;
    const t = (cup.tilt * Math.PI) / 180;
    const dx = Math.sin(t);
    const x0 = cup.x + dx * r;
    const y0 = cup.y - Math.cos(t) * r;
    const xExit = LANES_X[cup.lane];
    const yExit = HERO_H;

    const lipOut = 14;
    const xElbow = x0 + dx * lipOut;
    const yElbow = y0 + (cup.y < HERO_H / 2 ? 60 : 30);

    const verticalHold = Math.min(140, (yExit - yElbow) * 0.35);
    return [
      `M ${x0} ${y0}`,
      `C ${xElbow} ${y0} ${xElbow} ${yElbow - 30} ${xElbow} ${yElbow}`,
      `C ${xElbow} ${yElbow + verticalHold} ${xExit} ${
        yExit - verticalHold
      } ${xExit} ${yExit}`,
    ].join(" ");
  };

  return (
    <section className="act act-hero" data-screen-label="01 Hero">
      <div className="hero-wrap">
        <svg
          className="hero-canvas"
          viewBox={`0 0 ${HERO_W} ${HERO_H}`}
          preserveAspectRatio="xMidYMid meet"
          aria-hidden
        >
          <defs>
            <linearGradient
              id="cc-act1-line-fade"
              x1="0"
              y1="0"
              x2="0"
              y2={HERO_H}
              gradientUnits="userSpaceOnUse"
            >
              <stop offset="0" stopColor="white" />
              <stop offset="0.96" stopColor="white" />
              <stop offset="1" stopColor="black" />
            </linearGradient>
            <mask
              id="cc-act1-line-mask"
              maskUnits="userSpaceOnUse"
              x="0"
              y="0"
              width={HERO_W}
              height={HERO_H}
            >
              <rect
                x="0"
                y="0"
                width={HERO_W}
                height={HERO_H}
                fill="url(#cc-act1-line-fade)"
              />
            </mask>
          </defs>
          <g mask="url(#cc-act1-line-mask)">
            {cups.map((c) => (
              <path
                key={"pour-" + c.key}
                d={pourPath(c)}
                stroke="var(--cc-ink)"
                strokeWidth="var(--cc-line-w)"
                vectorEffect="non-scaling-stroke"
                fill="none"
                strokeLinecap="round"
                strokeLinejoin="round"
                opacity="0.95"
              />
            ))}
          </g>
        </svg>

        <div className="hero-copy">
          <div className="eyebrow">The API platform</div>
          <h1 className="display">
            The API Platform
            <br />
            <span className="accent">for Humans &amp; Agents</span>
          </h1>
          <p>
            Unify all your APIs into a comprehensive company graph, streamlining
            data accessibility and enhancing integration.
          </p>
          <div className="cta-stack">
            <button className="btn btn-primary">Start pouring →</button>
            <button className="btn btn-ghost">Read the docs</button>
          </div>
        </div>

        {cups.map((c) => (
          <div
            key={"cup-" + c.key}
            className="cup-pos-scatter"
            style={{
              left: `${(c.x / HERO_W) * 100}%`,
              top: `${(c.y / HERO_H) * 100}%`,
            }}
          >
            <Cup tilt={c.tilt} />
          </div>
        ))}
      </div>
    </section>
  );
};
