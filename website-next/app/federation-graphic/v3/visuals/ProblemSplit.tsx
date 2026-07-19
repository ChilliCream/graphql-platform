/**
 * Section scene: the monolithic API schema splitting into team-owned schemas.
 * Static composition; the section's RevealOnScroll provides the entrance.
 */

import {
  CARD_BG,
  CARD_BORDER,
  CODE,
  CYAN,
  GREEN,
  MONO_FONT,
  SLATE,
  VIOLET,
} from "../palette";

const MONO_FIELDS = [
  "name: String!",
  "stock: Int!",
  "rating: Float!",
  "price: Money!",
  "invoice: Invoice!",
  "history: [Order!]!",
];

const TEAMS = [
  {
    team: "catalog",
    color: CYAN,
    y: 40,
    fields: ["name: String!", "price: Money!"],
  },
  {
    team: "inventory",
    color: GREEN,
    y: 160,
    fields: ["stock: Int!", "warehouse: ID!"],
  },
  {
    team: "reviews",
    color: VIOLET,
    y: 280,
    fields: ["rating: Float!", "review: [Review!]"],
  },
];

export function ProblemSplit() {
  return (
    <svg aria-hidden="true" className="h-auto w-full" viewBox="0 0 900 420">
      {/* The monolith: one schema, one queue, every team waiting. */}
      <g>
        <rect
          x={30}
          y={70}
          width={300}
          height={280}
          rx={14}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <circle cx={52} cy={92} r={4} fill={SLATE} />
        <text
          x={66}
          y={96}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="11"
          fontWeight="600"
          letterSpacing="0.22em"
        >
          THE API TEAM
        </text>
        <text
          x={310}
          y={96}
          textAnchor="end"
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10"
          opacity="0.7"
        >
          schema.graphql
        </text>
        <line x1={30} x2={330} y1={108} y2={108} stroke={CARD_BORDER} />
        <text x={50} y={132} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          {"type Product {"}
        </text>
        {MONO_FIELDS.map((f, i) => (
          <text
            key={f}
            x={70}
            y={154 + i * 22}
            fill={CODE}
            fontFamily={MONO_FONT}
            fontSize="13"
          >
            {f}
          </text>
        ))}
        <text
          x={50}
          y={154 + MONO_FIELDS.length * 22}
          fill={CODE}
          fontFamily={MONO_FONT}
          fontSize="13"
        >
          {"}"}
        </text>
        <text
          x={50}
          y={332}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10.5"
          opacity="0.85"
        >
          every change queues behind one team
        </text>
      </g>

      {/* Split lines. */}
      {TEAMS.map((t) => (
        <path
          key={t.team}
          d={`M 330 210 C 420 210, 470 ${t.y + 55}, 540 ${t.y + 55}`}
          fill="none"
          stroke={t.color}
          strokeWidth="2"
          strokeOpacity="0.6"
        />
      ))}

      {/* Team-owned slices. */}
      {TEAMS.map((t) => (
        <g key={t.team}>
          <rect
            x={540}
            y={t.y}
            width={330}
            height={110}
            rx={14}
            fill={CARD_BG}
            stroke={CARD_BORDER}
          />
          <circle cx={562} cy={t.y + 22} r={4} fill={t.color} />
          <text
            x={576}
            y={t.y + 26}
            fill={SLATE}
            fontFamily={MONO_FONT}
            fontSize="11"
            fontWeight="600"
            letterSpacing="0.22em"
          >
            {t.team.toUpperCase()}
          </text>
          <text
            x={850}
            y={t.y + 26}
            textAnchor="end"
            fill={SLATE}
            fontFamily={MONO_FONT}
            fontSize="10"
            opacity="0.7"
          >
            deploys independently
          </text>
          <line
            x1={540}
            x2={870}
            y1={t.y + 36}
            y2={t.y + 36}
            stroke={CARD_BORDER}
          />
          {t.fields.map((f, i) => (
            <text
              key={f}
              x={560}
              y={t.y + 60 + i * 22}
              fill={CODE}
              fontFamily={MONO_FONT}
              fontSize="13"
            >
              {f}
            </text>
          ))}
        </g>
      ))}
    </svg>
  );
}
