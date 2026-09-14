"use client";

import { useRef } from "react";

import { Eyebrow } from "@/src/design-system/Eyebrow";

import { anim, useCycle, useElementMotion } from "../../hooks";
import { MC, SOURCES, STATIONS, specTag } from "../../palette";
import {
  APOLLO_FED,
  C,
  CLIENT_EDGE,
  CLIENT_NODES,
  FAN_OUT,
  FLIGHTS,
  GQL_FED,
  HUB_EDGE,
  KEYFRAMES,
  LANGUAGE,
  MERGE,
  META_SIZE,
  NAME_SIZE,
  REQUEST,
  RESPONSE,
  REST_STEP,
  RETURN,
  R_CLIENT,
  R_HUB,
  R_SOURCE,
  R_SUB,
  SOURCE_ANGLES,
  SOURCE_LANGUAGE,
  SPEC_COLOR,
  STEP_MS,
  SUB_ANGLES,
  SUB_EDGE,
  VIEW,
  arc,
  place,
  spoke,
} from "./board";
import { Light, Plate } from "./parts";

/**
 * Hero visual: the radial hub. The Fusion gateway is the centre of the board,
 * the five subgraphs sit on an inner ring - each a plate with its name, the
 * language it is written in and the federation specification it implements,
 * on a ring segment tinted per specification - the two non-GraphQL sources sit
 * just outside it as smaller satellites, and the four clients form the outer
 * ring. Requests run along the spokes: in from a client, out to the subgraphs
 * that query needs, back to the centre, then out as one merged response.
 *
 * The nodes are DOM text, not SVG text, so their size is fixed in px and never
 * shrinks with the box: the 11px floor holds at 375px without letterboxing.
 * The SVG below them is square (`1000 x 1000` in a square box), so rings and
 * spokes scale uniformly and carry no lettering of their own.
 */
export default function RadialHub() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const step = useCycle(running, FLIGHTS.length, STEP_MS, REST_STEP);
  const flight = FLIGHTS[step];
  const client = CLIENT_NODES[flight.client];
  const targets: readonly number[] = flight.targets;

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(60% 60% at 68% 50%, color-mix(in srgb, ${MC.phosphor} 12%, transparent), transparent)`,
        }}
      />

      <div className="absolute inset-0 flex items-center justify-center lg:justify-end lg:pr-[4%]">
        <div
          className="relative aspect-square"
          style={{ width: "min(100%, 88svh)" }}
        >
          <svg
            viewBox={`0 0 ${VIEW} ${VIEW}`}
            className="absolute inset-0 h-full w-full"
          >
            <g
              style={{
                transformBox: "view-box",
                transformOrigin: `${C}px ${C}px`,
                animation: anim(running, "rh-spin 140s linear infinite"),
                transform: running ? undefined : "rotate(-12deg)",
              }}
            >
              <circle
                cx={C}
                cy={C}
                r={R_CLIENT * 10}
                fill="none"
                stroke={MC.line}
                strokeOpacity="0.4"
                strokeDasharray="2 14"
              />
            </g>
            <g
              style={{
                transformBox: "view-box",
                transformOrigin: `${C}px ${C}px`,
                animation: anim(running, "rh-spin-back 100s linear infinite"),
                transform: running ? undefined : "rotate(8deg)",
              }}
            >
              <circle
                cx={C}
                cy={C}
                r={R_SOURCE * 10}
                fill="none"
                stroke={MC.line}
                strokeOpacity="0.22"
                strokeDasharray="18 10"
              />
            </g>

            {STATIONS.map((station, i) => (
              <path
                key={`arc-${station.name}`}
                d={arc(SUB_ANGLES[i] - 34, SUB_ANGLES[i] + 34, R_SUB * 10)}
                fill="none"
                stroke={SPEC_COLOR[station.spec]}
                strokeOpacity="0.3"
                strokeWidth="26"
                strokeLinecap="round"
              />
            ))}

            {STATIONS.map((station, i) => (
              <path
                key={`spoke-${station.name}`}
                d={spoke(SUB_ANGLES[i], R_SUB, 62)}
                stroke={SPEC_COLOR[station.spec]}
                style={{
                  strokeOpacity: targets.includes(i) ? 0.85 : 0.25,
                  strokeWidth: targets.includes(i) ? 2 : 1,
                  transition: "stroke-opacity 400ms, stroke-width 400ms",
                }}
              />
            ))}
            {SOURCES.map((source, i) => (
              <path
                key={`spoke-${source.name}`}
                d={spoke(SOURCE_ANGLES[i], R_SOURCE, 44)}
                stroke={MC.line}
                strokeOpacity="0.75"
                strokeDasharray="5 7"
              />
            ))}
            {CLIENT_NODES.map((node) => (
              <path
                key={`spoke-${node.name}`}
                d={spoke(node.angle, R_CLIENT, 50)}
                stroke={MC.signal}
                style={{
                  strokeOpacity: node.name === client.name ? 0.8 : 0.2,
                  strokeWidth: node.name === client.name ? 2 : 1,
                  transition: "stroke-opacity 400ms, stroke-width 400ms",
                }}
              />
            ))}

            <g key={step}>
              <Light
                angle={client.angle}
                from={CLIENT_EDGE}
                to={HUB_EDGE}
                color={MC.signal}
                delay={REQUEST.delay}
                duration={REQUEST.duration}
                rest={0.5}
                running={running}
              />
              {targets.map((target, i) => (
                <Light
                  key={`out-${STATIONS[target].name}`}
                  angle={SUB_ANGLES[target]}
                  from={HUB_EDGE}
                  to={SUB_EDGE}
                  color={MC.signal}
                  delay={FAN_OUT.delay + i * FAN_OUT.stagger}
                  duration={FAN_OUT.duration}
                  rest={0.5}
                  running={running}
                />
              ))}
              {targets.map((target) => (
                <Light
                  key={`back-${STATIONS[target].name}`}
                  angle={SUB_ANGLES[target]}
                  from={SUB_EDGE}
                  to={HUB_EDGE}
                  color={SPEC_COLOR[STATIONS[target].spec]}
                  delay={RETURN.delay}
                  duration={RETURN.duration}
                  rest={false}
                  running={running}
                />
              ))}
              <Light
                angle={client.angle}
                from={HUB_EDGE}
                to={CLIENT_EDGE}
                color={MC.phosphor}
                delay={RESPONSE.delay}
                duration={RESPONSE.duration}
                rest={false}
                running={running}
              />
              <circle
                cx={C}
                cy={C}
                r={R_HUB * 10}
                fill="none"
                stroke={MC.phosphor}
                strokeWidth="3"
                style={{
                  transformBox: "view-box",
                  transformOrigin: `${C}px ${C}px`,
                  animation: anim(
                    running,
                    `rh-merge ${MERGE.duration}ms ease-out ${MERGE.delay}ms both`,
                  ),
                  opacity: running ? undefined : 0,
                }}
              />
            </g>
          </svg>

          <div
            className="absolute flex flex-col items-center justify-center rounded-full border text-center"
            style={{
              left: "50%",
              top: "50%",
              width: `${R_HUB * 2}%`,
              height: `${R_HUB * 2}%`,
              transform: "translate(-50%, -50%)",
              background: MC.panel,
              borderColor: `color-mix(in srgb, ${MC.phosphor} 60%, transparent)`,
              fontFamily: MC.mono,
              letterSpacing: "0.12em",
              textTransform: "uppercase",
            }}
          >
            <span style={{ color: MC.ink, fontSize: NAME_SIZE }}>Fusion</span>
            <span style={{ color: MC.dim, fontSize: META_SIZE }}>Gateway</span>
            <span
              className="mt-1 px-2 leading-tight"
              style={{ color: MC.phosphor, fontSize: META_SIZE }}
            >
              Composite schema
            </span>
          </div>

          {STATIONS.map((station, i) => (
            <Plate
              key={station.name}
              style={place(SUB_ANGLES[i], R_SUB)}
              tone={SPEC_COLOR[station.spec]}
              active={targets.includes(i)}
            >
              <span
                className="block"
                style={{ color: MC.ink, fontSize: NAME_SIZE }}
              >
                {station.name}
              </span>
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {LANGUAGE[station.name]}
              </span>
              <span
                className="block"
                style={{
                  color: SPEC_COLOR[station.spec],
                  fontSize: META_SIZE,
                }}
              >
                {specTag(station.spec)}
              </span>
            </Plate>
          ))}

          {SOURCES.map((source, i) => (
            <Plate
              key={source.name}
              style={place(SOURCE_ANGLES[i], R_SOURCE)}
              tone={MC.panelEdge}
            >
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {source.name}
              </span>
              <span
                className="block"
                style={{ color: MC.ink, fontSize: META_SIZE }}
              >
                {source.kind}
              </span>
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {SOURCE_LANGUAGE[source.kind]}
              </span>
            </Plate>
          ))}

          {CLIENT_NODES.map((node) => (
            <Plate
              key={node.name}
              style={place(node.angle, R_CLIENT)}
              tone={MC.signal}
              active={node.name === client.name}
            >
              <span
                className="block"
                style={{ color: MC.ink, fontSize: NAME_SIZE }}
              >
                {node.name}
              </span>
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {node.note}
              </span>
            </Plate>
          ))}
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg absolute right-4 bottom-4 rounded-md border px-3 py-2 sm:right-8 sm:bottom-8">
        <Eyebrow color="ink-dim">Composite schema</Eyebrow>
        <ul
          className="mt-2 space-y-1"
          style={{ fontFamily: MC.mono, fontSize: META_SIZE }}
        >
          {[GQL_FED, APOLLO_FED].map((spec) => (
            <li key={spec} className="flex items-center gap-2">
              <span
                className="inline-block h-2 w-6 rounded-full"
                style={{ background: SPEC_COLOR[spec] }}
              />
              <span style={{ color: MC.ink }}>{spec}</span>
            </li>
          ))}
        </ul>
        <p
          className="mt-2"
          style={{ fontFamily: MC.mono, fontSize: META_SIZE, color: MC.dim }}
        >
          {`${STATIONS.length} subgraphs · ${SOURCES.length} sources`}
        </p>
        <p
          className="mt-1"
          style={{
            fontFamily: MC.mono,
            fontSize: META_SIZE,
            color: MC.phosphor,
          }}
        >
          {`${client.name} → ${targets
            .map((target) => STATIONS[target].name)
            .join(" + ")} → 1 response`}
        </p>
      </div>
    </div>
  );
}
