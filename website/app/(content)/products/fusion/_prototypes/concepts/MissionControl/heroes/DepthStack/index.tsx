"use client";

import { useRef } from "react";
import type { CSSProperties, ReactNode } from "react";

import { FONTS, TYPE } from "../../../../brand";
import { useCycle, useElementMotion } from "../../hooks";
import { MC, specTag } from "../../palette";
import type { Tilt } from "./motion";
import { Beam, KEYFRAMES, LEGS, Traveller, usePointerTilt } from "./motion";
import type { Spot, Stage } from "./scene";
import {
  at,
  CLIENT_CARDS,
  NARROW,
  PLANS,
  PLATES,
  SOURCE_CARDS,
  SPEC_ACCENT,
  targetPoint,
  WIDE,
} from "./scene";

/**
 * Hero visual: the depth stack. Three planes in perspective - four clients
 * nearest the viewer, the Fusion gateway in the middle, the five subgraphs
 * and the two non-GraphQL sources on the back plane - built from real DOM
 * panels with CSS 3D transforms, so every label stays selectable text at its
 * own size instead of shrinking with a viewBox.
 *
 * One client is on the air at a time: its card lights up, the gateway names
 * the plan it composed, and the two or three sources that plan needs light up
 * on the back plane.
 *
 * Below `md` the same three planes stack down the screen at a shallower angle
 * and a shallower depth, which is what keeps the smallest label above the
 * 11px floor on a 375px viewport (see `scene.ts` for the arithmetic).
 */

/** One step per client: a full round trip through the stack. */
const PERIOD = 4200;
const REST_STEP = 0;

const panelStyle = (spot: Spot, z: number): CSSProperties => ({
  position: "absolute",
  left: `${spot.x}%`,
  top: `${spot.y}%`,
  width: spot.w,
  height: spot.h,
  marginLeft: -spot.w / 2,
  marginTop: -spot.h / 2,
  transform: `translateZ(${z}px)`,
});

/** Panel fill: the console plate, washed with the accent while it is lit. */
const fill = (accent: string, lit: boolean) =>
  lit ? `color-mix(in srgb, ${accent} 14%, ${MC.panel})` : MC.panel;

const edge = (accent: string, lit: boolean) =>
  lit ? `color-mix(in srgb, ${accent} 70%, transparent)` : MC.panelEdge;

interface PlaneProps {
  readonly spot: Spot;
  readonly z: number;
  readonly accent: string;
  readonly lit: boolean;
  readonly radius: number;
  readonly children: ReactNode;
}

/** A plate on one of the three planes. */
function Panel({ spot, z, accent, lit, radius, children }: PlaneProps) {
  return (
    <div
      style={{
        ...panelStyle(spot, z),
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: 2,
        borderRadius: radius,
        borderWidth: 1,
        borderStyle: "solid",
        borderColor: edge(accent, lit),
        background: fill(accent, lit),
        boxShadow: lit
          ? `0 0 24px 0 color-mix(in srgb, ${accent} 22%, transparent)`
          : "none",
        transition: "background 420ms, border-color 420ms, box-shadow 420ms",
        fontFamily: MC.mono,
        textAlign: "center",
      }}
    >
      {children}
    </div>
  );
}

interface LineProps {
  readonly size: number;
  readonly color: string;
  readonly spaced?: boolean;
  readonly children: ReactNode;
}

/** One line of plate lettering, sized in px so its rendered size is known. */
function Line({ size, color, spaced, children }: LineProps) {
  return (
    <span
      style={{
        color,
        fontSize: size,
        lineHeight: 1.25,
        letterSpacing: spaced ? "0.1em" : "0.02em",
        whiteSpace: "nowrap",
      }}
    >
      {children}
    </span>
  );
}

interface StageViewProps {
  readonly stage: Stage;
  readonly step: number;
  readonly running: boolean;
  readonly tilt: Tilt;
}

/** One layout of the scene: the three planes inside their own perspective. */
function StageView({ stage, step, running, tilt }: StageViewProps) {
  const wide = stage.variant === "wide";
  const plan = PLANS[step];
  const client = CLIENT_CARDS[step];
  const gateway = at(stage.gateway, 0);
  const onAir = at(stage.clients[step], stage.clientZ);

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        perspective: `${stage.perspective}px`,
        perspectiveOrigin: "50% 46%",
      }}
    >
      <div
        style={{
          position: "absolute",
          inset: 0,
          transformStyle: "preserve-3d",
          transform: `rotateX(${stage.tilt + tilt.x}deg) rotateY(${tilt.y}deg)`,
          transition: "transform 260ms ease-out",
        }}
      >
        <div
          style={{
            position: "absolute",
            left: "50%",
            top: "46%",
            width: "140%",
            height: "80%",
            marginLeft: "-70%",
            marginTop: "-40%",
            transform: `translateZ(${stage.backZ - 90}px)`,
            backgroundImage: `linear-gradient(${MC.grid} 1px, transparent 1px), linear-gradient(90deg, ${MC.grid} 1px, transparent 1px)`,
            backgroundSize: "56px 56px",
          }}
        />

        {CLIENT_CARDS.map((card, i) => (
          <Beam
            key={`link-${card.name}`}
            from={at(stage.clients[i], stage.clientZ)}
            to={gateway}
            color={MC.signal}
            lit={i === step}
          />
        ))}

        {PLATES.map((plate, i) => (
          <Beam
            key={`link-${plate.name}`}
            from={gateway}
            to={at(stage.plates[i], stage.backZ)}
            color={MC.phosphor}
            lit={plan.includes(plate.name)}
          />
        ))}

        {SOURCE_CARDS.map((source, i) => (
          <Beam
            key={`link-${source.name}`}
            from={gateway}
            to={at(stage.sources[i], stage.backZ)}
            color={MC.phosphor}
            lit={plan.includes(source.name)}
          />
        ))}

        {PLATES.map((plate, i) => {
          const lit = plan.includes(plate.name);
          return (
            <Panel
              key={plate.name}
              spot={stage.plates[i]}
              z={stage.backZ}
              accent={SPEC_ACCENT[plate.spec]}
              lit={lit}
              radius={10}
            >
              {wide ? (
                <>
                  <Line size={TYPE.h6} color={MC.ink}>
                    {plate.name}
                  </Line>
                  <Line size={TYPE.caption} color={MC.dim}>
                    {plate.language}
                  </Line>
                  <Line
                    size={TYPE.caption}
                    color={lit ? SPEC_ACCENT[plate.spec] : MC.dim}
                    spaced
                  >
                    {plate.spec}
                  </Line>
                </>
              ) : (
                <>
                  <Line size={TYPE.caption} color={MC.ink}>
                    {`${plate.name} · ${plate.language}`}
                  </Line>
                  <Line
                    size={TYPE.caption}
                    color={lit ? SPEC_ACCENT[plate.spec] : MC.dim}
                    spaced
                  >
                    {specTag(plate.spec)}
                  </Line>
                </>
              )}
            </Panel>
          );
        })}

        {SOURCE_CARDS.map((source, i) => {
          const lit = plan.includes(source.name);
          return (
            <Panel
              key={source.name}
              spot={stage.sources[i]}
              z={stage.backZ}
              accent={MC.phosphor}
              lit={lit}
              radius={10}
            >
              {wide ? (
                <>
                  <Line size={TYPE.body} color={MC.ink}>
                    {source.name}
                  </Line>
                  <Line size={TYPE.caption} color={MC.dim} spaced>
                    {source.kind}
                  </Line>
                  <Line size={TYPE.caption} color={MC.dim}>
                    {source.language}
                  </Line>
                </>
              ) : (
                <Line size={TYPE.caption} color={MC.dim} spaced>
                  {`${source.kind} · ${source.language}`}
                </Line>
              )}
            </Panel>
          );
        })}

        <Panel spot={stage.gateway} z={0} accent={MC.phosphor} lit radius={14}>
          <span
            style={{
              color: MC.ink,
              fontFamily: FONTS.heading,
              fontSize: wide ? TYPE.h5 : TYPE.h6,
              lineHeight: 1.1,
            }}
          >
            Fusion
          </span>
          <Line size={TYPE.caption} color={MC.dim} spaced>
            ONE COMPOSITE SCHEMA
          </Line>
          <Line size={TYPE.caption} color={MC.phosphor}>
            {`${client.name} → ${plan.length} sources`}
          </Line>
        </Panel>

        {CLIENT_CARDS.map((card, i) => (
          <Panel
            key={card.name}
            spot={stage.clients[i]}
            z={stage.clientZ}
            accent={MC.signal}
            lit={i === step}
            radius={card.radius}
          >
            <Line size={wide ? TYPE.body : TYPE.caption} color={MC.ink}>
              {card.name}
            </Line>
            <Line size={TYPE.label} color={MC.dim} spaced>
              {card.note}
            </Line>
          </Panel>
        ))}

        <Traveller
          key={`request-${step}`}
          from={onAir}
          to={gateway}
          color={MC.signal}
          leg={LEGS.request}
          rest={0.5}
          running={running}
          periodMs={PERIOD}
        />
        {plan.map((name) => (
          <Traveller
            key={`fan-out-${step}-${name}`}
            from={gateway}
            to={targetPoint(stage, name)}
            color={MC.signal}
            leg={LEGS.fanOut}
            rest={0.62}
            running={running}
            periodMs={PERIOD}
          />
        ))}
        {plan.map((name) => (
          <Traveller
            key={`collect-${step}-${name}`}
            from={targetPoint(stage, name)}
            to={gateway}
            color={MC.phosphor}
            leg={LEGS.collect}
            rest={0.7}
            running={running}
            periodMs={PERIOD}
          />
        ))}
        <Traveller
          key={`answer-${step}`}
          from={gateway}
          to={onAir}
          color={MC.phosphor}
          leg={LEGS.answer}
          rest={0.24}
          running={running}
          periodMs={PERIOD}
        />
      </div>
    </div>
  );
}

/** The narrow stack stays square on; only the wide one follows the pointer. */
const LEVEL = { x: 0, y: 0 } as const;

export default function DepthStack() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const step = useCycle(running, CLIENT_CARDS.length, PERIOD, REST_STEP);
  const tilt = usePointerTilt(running, WIDE.swing);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <div className="pointer-events-none absolute inset-0 md:hidden">
        <StageView stage={NARROW} step={step} running={running} tilt={LEVEL} />
      </div>
      <div className="pointer-events-none absolute inset-0 hidden md:block lg:left-[26%]">
        <StageView stage={WIDE} step={step} running={running} tilt={tilt} />
      </div>
    </div>
  );
}
