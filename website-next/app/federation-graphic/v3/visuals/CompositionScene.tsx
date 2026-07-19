/**
 * Section scene: composition as a step. Three source schemas feed a validated
 * compose step; out comes the one schema clients query, with every field
 * carrying its owner's chip.
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

const SOURCES = [
  { team: "catalog", color: CYAN, y: 50 },
  { team: "inventory", color: GREEN, y: 150 },
  { team: "reviews", color: VIOLET, y: 250 },
];

const MERGED = [
  { code: "type Product {" },
  { code: "  id: ID!" },
  { code: "  name: String!", color: CYAN },
  { code: "  price: Money!", color: CYAN },
  { code: "  stock: Int!", color: GREEN },
  { code: "  rating: Float!", color: VIOLET },
  { code: "}" },
];

export function CompositionScene() {
  return (
    <svg aria-hidden="true" className="h-auto w-full" viewBox="0 0 900 380">
      {/* Compact source schemas. */}
      {SOURCES.map((s) => (
        <g key={s.team}>
          <rect
            x={30}
            y={s.y}
            width={230}
            height={64}
            rx={12}
            fill={CARD_BG}
            stroke={CARD_BORDER}
          />
          <circle cx={52} cy={s.y + 22} r={4} fill={s.color} />
          <text
            x={66}
            y={s.y + 26}
            fill={SLATE}
            fontFamily={MONO_FONT}
            fontSize="10"
            fontWeight="600"
            letterSpacing="0.18em"
          >
            {s.team.toUpperCase()}
          </text>
          <text
            x={50}
            y={s.y + 48}
            fill={CODE}
            fontFamily={MONO_FONT}
            fontSize="12"
          >
            {"type Product { … }"}
          </text>
          <path
            d={`M 260 ${s.y + 32} C 320 ${s.y + 32}, 330 182, 388 182`}
            fill="none"
            stroke={s.color}
            strokeWidth="2"
            strokeOpacity="0.65"
          />
        </g>
      ))}

      {/* The compose step. */}
      <g>
        <rect
          x={388}
          y={150}
          width={132}
          height={64}
          rx={12}
          fill={CARD_BG}
          stroke={TEAL}
          strokeOpacity="0.5"
        />
        <text
          x={454}
          y={178}
          textAnchor="middle"
          fill={TEAL}
          fontFamily={MONO_FONT}
          fontSize="11"
          fontWeight="600"
          letterSpacing="0.18em"
        >
          COMPOSE
        </text>
        <text
          x={454}
          y={198}
          textAnchor="middle"
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="9.5"
        >
          merge + validate
        </text>
      </g>
      <path
        d="M 520 182 L 580 182"
        stroke={TEAL}
        strokeWidth="2"
        strokeOpacity="0.65"
      />

      {/* The composite schema. */}
      <g>
        <rect
          x={580}
          y={70}
          width={290}
          height={224}
          rx={14}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <text
          x={600}
          y={98}
          fill={INK}
          fontFamily={MONO_FONT}
          fontSize="12"
          fontWeight="600"
        >
          composite schema
        </text>
        <text
          x={850}
          y={98}
          textAnchor="end"
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="9.5"
          opacity="0.8"
        >
          aka supergraph
        </text>
        <line x1={580} x2={870} y1={110} y2={110} stroke={CARD_BORDER} />
        {MERGED.map((l, i) => (
          <g key={l.code}>
            <text
              x={600}
              y={136 + i * 22}
              fill={CODE}
              fontFamily={MONO_FONT}
              fontSize="12.5"
            >
              {l.code}
            </text>
            {l.color && (
              <circle cx={836} cy={132 + i * 22} r={3.5} fill={l.color} />
            )}
          </g>
        ))}
      </g>

      <text
        x={450}
        y={356}
        textAnchor="middle"
        fill={SLATE}
        fontFamily={MONO_FONT}
        fontSize="11"
        letterSpacing="0.24em"
      >
        VALIDATED BEFORE ANYTHING DEPLOYS
      </text>
    </svg>
  );
}
