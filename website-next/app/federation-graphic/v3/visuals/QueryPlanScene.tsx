/**
 * Section scene: one query planned across three services and merged into one
 * response. Query card left, service nodes center, response card right.
 */

import {
  CARD_BG,
  CARD_BORDER,
  CODE,
  CYAN,
  GREEN,
  INK,
  MONO_FONT,
  SLATE,
  TEAL,
  VIOLET,
} from "../palette";

const QUERY_LINES = [
  { code: "{", indent: 0 },
  { code: 'product(id: "P-401") {', indent: 1, color: TEAL },
  { code: "name", indent: 2, color: CYAN },
  { code: "stock", indent: 2, color: GREEN },
  { code: "rating", indent: 2, color: VIOLET },
  { code: "}", indent: 1 },
  { code: "}", indent: 0 },
];

const SERVICES = [
  { team: "catalog", color: CYAN, y: 60 },
  { team: "inventory", color: GREEN, y: 180 },
  { team: "reviews", color: VIOLET, y: 300 },
];

const RESPONSE_LINES = [
  '{ "product": {',
  '    "name": "Aero Mug",',
  '    "stock": 12,',
  '    "rating": 4.8',
  "} }",
];

export function QueryPlanScene() {
  return (
    <svg aria-hidden="true" className="h-auto w-full" viewBox="0 0 900 420">
      {/* Query card. */}
      <g>
        <rect
          x={30}
          y={110}
          width={250}
          height={200}
          rx={14}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <text
          x={50}
          y={138}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.22em"
        >
          ONE REQUEST
        </text>
        <line x1={30} x2={280} y1={148} y2={148} stroke={CARD_BORDER} />
        {QUERY_LINES.map((l, i) => (
          <text
            key={i}
            x={50 + l.indent * 16}
            y={172 + i * 20}
            fill={l.color ?? CODE}
            fontFamily={MONO_FONT}
            fontSize="12.5"
          >
            {l.code}
          </text>
        ))}
      </g>

      {/* Plan fan-out. */}
      {SERVICES.map((s) => (
        <path
          key={s.team}
          d={`M 280 210 C 340 210, 360 ${s.y + 30}, 410 ${s.y + 30}`}
          fill="none"
          stroke={s.color}
          strokeWidth="2"
          strokeOpacity="0.7"
        />
      ))}

      {/* Services. */}
      {SERVICES.map((s) => (
        <g key={s.team}>
          <rect
            x={410}
            y={s.y}
            width={180}
            height={60}
            rx={12}
            fill={CARD_BG}
            stroke={CARD_BORDER}
          />
          <circle cx={432} cy={s.y + 30} r={4} fill={s.color} />
          <text
            x={448}
            y={s.y + 34}
            fill={CODE}
            fontFamily={MONO_FONT}
            fontSize="12"
          >
            {s.team}
          </text>
        </g>
      ))}

      {/* Merge. */}
      {SERVICES.map((s) => (
        <path
          key={s.team}
          d={`M 590 ${s.y + 30} C 640 ${s.y + 30}, 650 210, 680 210`}
          fill="none"
          stroke={s.color}
          strokeWidth="2"
          strokeOpacity="0.7"
        />
      ))}

      {/* Response card. */}
      <g>
        <rect
          x={680}
          y={130}
          width={200}
          height={160}
          rx={14}
          fill={CARD_BG}
          stroke={TEAL}
          strokeOpacity="0.5"
        />
        <text
          x={700}
          y={158}
          fill={TEAL}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.22em"
        >
          ONE RESPONSE
        </text>
        <line x1={680} x2={880} y1={168} y2={168} stroke={CARD_BORDER} />
        {RESPONSE_LINES.map((l, i) => (
          <text
            key={i}
            x={700}
            y={192 + i * 20}
            fill={i === 0 || i === 4 ? CODE : INK}
            fontFamily={MONO_FONT}
            fontSize="11.5"
          >
            {l}
          </text>
        ))}
      </g>

      <text
        x={450}
        y={400}
        textAnchor="middle"
        fill={SLATE}
        fontFamily={MONO_FONT}
        fontSize="11"
        letterSpacing="0.24em"
      >
        THE GATEWAY PLANS · CALLS · MERGES
      </text>
    </svg>
  );
}
