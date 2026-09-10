"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { BP, PARTS } from "./palette";
import { draw, fade, Sheet, stamp } from "./Sheet";

/**
 * "What is Fusion?": the query drawn as a dimension chain. One client sends
 * one query into the gateway, which works out which sub-assembly holds each
 * measurement, takes one dimension off each of them, and closes the chain with
 * a single overall dimension - one response, off one endpoint.
 *
 * Rest state: the chain fully dimensioned, every segment labelled with the
 * field it measures and the overall dimension drawn underneath.
 */

/** Witness lines of the chain; each pair of neighbours bounds one segment. */
const WITNESS = [22, 128, 240, 352, 458];
const CHAIN_Y = 200;
const OVERALL_Y = 232;
const BOX_TOP = 108;
const BOX_BOTTOM = 150;

const SEGMENTS = PARTS.slice(0, 4).map((part, i) => ({
  ...part,
  left: WITNESS[i],
  right: WITNESS[i + 1],
  cx: (WITNESS[i] + WITNESS[i + 1]) / 2,
}));

/** A dimension arrowhead at `x`, pointing left (-1) or right (1). */
function head(x: number, y: number, dir: 1 | -1): string {
  return `M${x} ${y}l${dir * 7} -2.6v5.2Z`;
}

const CSS = `
${draw("bp-d-client", 2, 5)}
${fade("bp-d-client-label", 6)}
${draw("bp-d-query", 8, 5)}
${draw("bp-d-gateway", 14, 6)}
${fade("bp-d-gateway-label", 20)}
${SEGMENTS.map((_, i) => draw(`bp-d-leader${i}`, 24 + i * 2.5, 4)).join("\n")}
${SEGMENTS.map((_, i) => draw(`bp-d-box${i}`, 26 + i * 2.5, 5)).join("\n")}
${SEGMENTS.map((_, i) => fade(`bp-d-name${i}`, 30 + i * 2.5)).join("\n")}
${SEGMENTS.map((_, i) => draw(`bp-d-wit${i}`, 44 + i * 3, 4)).join("\n")}
${SEGMENTS.map((_, i) => draw(`bp-d-seg${i}`, 47 + i * 3, 4)).join("\n")}
${SEGMENTS.map((_, i) => fade(`bp-d-val${i}`, 50 + i * 3)).join("\n")}
${draw("bp-d-overall", 64, 6)}
${fade("bp-d-overall-label", 71)}
${draw("bp-d-return", 74, 5)}
${stamp("bp-d-stamp", 82)}
${fade("bp-d-note", 88)}
`;

export function DimensionChain() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();

  return (
    <Sheet
      title="Query resolution"
      no="DWG-101"
      rev="A"
      run={active && !reduced}
    >
      <svg
        viewBox="0 0 480 270"
        preserveAspectRatio="xMidYMid meet"
        className="absolute inset-0 h-full w-full"
      >
        <style>{CSS}</style>

        {/* The client and its one query */}
        <path
          className="bp-d-client"
          d="M14 10h86v30H14Z"
          pathLength={1}
          fill="none"
          stroke={BP.ink}
          strokeWidth={1.2}
        />
        <g className="bp-d-client-label">
          <text x={24} y={24} fontSize={8}>
            WEB CLIENT
          </text>
          <text className="bp-t-dim" x={24} y={35} fontSize={6}>
            ONE QUERY
          </text>
        </g>
        <path
          className="bp-d-query"
          d="M100 25h140v14"
          pathLength={1}
          fill="none"
          stroke={BP.dim}
          strokeWidth={1}
        />
        <path
          className="bp-d-gateway-label"
          d="M240 46l-2.8-7h5.6Z"
          fill={BP.dim}
        />

        {/* The main assembly */}
        <path
          className="bp-d-gateway"
          d="M22 46h436v30H22Z"
          pathLength={1}
          fill={BP.plate}
          stroke={BP.ink}
          strokeWidth={1.6}
        />
        <g className="bp-d-gateway-label">
          <text x={240} y={65} textAnchor="middle" fontSize={10}>
            FUSION GATEWAY
          </text>
          <text
            className="bp-t-dim"
            x={450}
            y={65}
            textAnchor="end"
            fontSize={6}
          >
            ASSY-100
          </text>
          <text className="bp-t-dim" x={30} y={65} fontSize={6}>
            ONE ENDPOINT
          </text>
        </g>

        {/* Sub-assemblies, one per segment of the chain */}
        {SEGMENTS.map((segment, i) => (
          <g key={segment.no}>
            <path
              className={`bp-d-leader${i}`}
              d={`M${segment.cx} 76v${BOX_TOP - 76}`}
              pathLength={1}
              fill="none"
              stroke={BP.inkFaint}
              strokeWidth={0.9}
            />
            <path
              className={`bp-d-box${i}`}
              d={`M${segment.cx - 48} ${BOX_TOP}h96v${BOX_BOTTOM - BOX_TOP}h-96Z`}
              pathLength={1}
              fill={BP.plate}
              stroke={BP.ink}
              strokeWidth={1.2}
            />
            <g className={`bp-d-name${i}`}>
              <text x={segment.cx} y={126} textAnchor="middle" fontSize={9}>
                {segment.name.toUpperCase()}
              </text>
              <text
                className="bp-t-dim"
                x={segment.cx}
                y={140}
                textAnchor="middle"
                fontSize={6}
              >
                {`${segment.no} · ${segment.language}`}
              </text>
            </g>
          </g>
        ))}

        {/* The chain: one dimension per sub-assembly */}
        {SEGMENTS.map((segment, i) => (
          <g key={`dim-${segment.no}`}>
            <path
              className={`bp-d-wit${i}`}
              d={`M${segment.left} ${BOX_BOTTOM + 4}v${CHAIN_Y - BOX_BOTTOM + 8}`}
              pathLength={1}
              fill="none"
              stroke={BP.inkFaint}
              strokeWidth={0.8}
            />
            <path
              className={`bp-d-seg${i}`}
              d={`M${segment.left} ${CHAIN_Y}h${segment.right - segment.left}`}
              pathLength={1}
              fill="none"
              stroke={BP.dim}
              strokeWidth={1}
            />
            <g className={`bp-d-val${i}`}>
              <path d={head(segment.left, CHAIN_Y, 1)} fill={BP.dim} />
              <path d={head(segment.right, CHAIN_Y, -1)} fill={BP.dim} />
              <text
                className="bp-t-cyan"
                x={segment.cx}
                y={CHAIN_Y - 6}
                textAnchor="middle"
                fontSize={8}
              >
                {segment.feature}
              </text>
            </g>
          </g>
        ))}
        <path
          className="bp-d-wit3"
          d={`M${WITNESS[4]} ${BOX_BOTTOM + 4}v${CHAIN_Y - BOX_BOTTOM + 8}`}
          pathLength={1}
          fill="none"
          stroke={BP.inkFaint}
          strokeWidth={0.8}
        />

        {/* The overall dimension: one response */}
        <path
          className="bp-d-overall"
          d={`M22 ${OVERALL_Y}h436`}
          pathLength={1}
          fill="none"
          stroke={BP.ink}
          strokeWidth={1.2}
        />
        <g className="bp-d-overall-label">
          <path d={head(22, OVERALL_Y, 1)} fill={BP.ink} />
          <path d={head(458, OVERALL_Y, -1)} fill={BP.ink} />
          <text x={240} y={OVERALL_Y - 7} textAnchor="middle" fontSize={9}>
            ONE RESPONSE
          </text>
        </g>
        <path
          className="bp-d-return"
          d={`M22 ${OVERALL_Y}v14h-8`}
          pathLength={1}
          fill="none"
          stroke={BP.ink}
          strokeWidth={1}
        />

        <g className="bp-d-stamp" style={{ transformOrigin: "390px 258px" }}>
          <path
            d="M318 246h144v22H318Z"
            fill="none"
            stroke={BP.ok}
            strokeWidth={1}
          />
          <text
            className="bp-t-ok"
            x={390}
            y={261}
            textAnchor="middle"
            fontSize={7.5}
          >
            COMPOSED IN THE BUILD
          </text>
        </g>
        <text className="bp-d-note bp-t-dim" x={22} y={261} fontSize={6.5}>
          CHAIN CLOSES AT THE GATEWAY · NOT AT THE CLIENT
        </text>
      </svg>
    </Sheet>
  );
}
