/**
 * Section scene: the entity up close. One Product card, three identity badges
 * joined on id, per-field owner chips, dual-vocabulary captions.
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

const OWNERS = [
  { team: "catalog", color: CYAN, y: 80 },
  { team: "inventory", color: GREEN, y: 180 },
  { team: "reviews", color: VIOLET, y: 280 },
];

const ROWS = [
  { code: "id: ID!", isKey: true },
  { code: "name: String!", color: CYAN },
  { code: "price: Money!", color: CYAN },
  { code: "stock: Int!", color: GREEN },
  { code: "rating: Float!", color: VIOLET },
];

export function EntityZoom() {
  return (
    <svg aria-hidden="true" className="h-auto w-full" viewBox="0 0 900 400">
      {/* Identity badges: every team knows the same product. */}
      {OWNERS.map((o) => (
        <g key={o.team}>
          <rect
            x={40}
            y={o.y}
            width={230}
            height={54}
            rx={12}
            fill={CARD_BG}
            stroke={CARD_BORDER}
          />
          <circle cx={62} cy={o.y + 27} r={4} fill={o.color} />
          <text
            x={78}
            y={o.y + 23}
            fill={SLATE}
            fontFamily={MONO_FONT}
            fontSize="10"
            fontWeight="600"
            letterSpacing="0.18em"
          >
            {o.team.toUpperCase()}
          </text>
          <text
            x={78}
            y={o.y + 41}
            fill={CODE}
            fontFamily={MONO_FONT}
            fontSize="12.5"
          >
            {'id: "P-401"'}
          </text>
          <path
            d={`M 270 ${o.y + 27} C 340 ${o.y + 27}, 360 190, 430 190`}
            fill="none"
            stroke={o.color}
            strokeWidth="1.5"
            strokeDasharray="3 5"
            strokeOpacity="0.6"
          />
        </g>
      ))}
      <text
        x={40}
        y={368}
        fill={SLATE}
        fontFamily={MONO_FONT}
        fontSize="10.5"
        opacity="0.85"
      >
        same id · same product · different knowledge
      </text>

      {/* The composed entity. */}
      <g>
        <rect
          x={430}
          y={80}
          width={310}
          height={220}
          rx={14}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <rect
          x={423}
          y={73}
          width={324}
          height={234}
          rx={20}
          fill="none"
          stroke={TEAL}
          strokeOpacity="0.35"
          strokeWidth="2"
        />
        <text
          x={450}
          y={108}
          fill={INK}
          fontFamily={MONO_FONT}
          fontSize="14"
          fontWeight="600"
        >
          Product
        </text>
        <text
          x={720}
          y={108}
          textAnchor="end"
          fill={TEAL}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.22em"
        >
          ENTITY
        </text>
        <line x1={430} x2={740} y1={120} y2={120} stroke={CARD_BORDER} />
        {ROWS.map((r, j) => (
          <g key={r.code}>
            <text
              x={450}
              y={148 + j * 26}
              fill={r.isKey ? INK : CODE}
              fontFamily={MONO_FONT}
              fontSize="13.5"
            >
              {r.code}
            </text>
            {r.isKey ? (
              <g>
                <circle cx={664} cy={144} r={3.5} fill={CYAN} />
                <circle cx={678} cy={144} r={3.5} fill={GREEN} />
                <circle cx={692} cy={144} r={3.5} fill={VIOLET} />
              </g>
            ) : (
              <circle cx={692} cy={144 + j * 26} r={3.5} fill={r.color} />
            )}
          </g>
        ))}
      </g>

      {/* Dual vocabulary. */}
      <text x={430} y={344} fill={SLATE} fontFamily={MONO_FONT} fontSize="10.5">
        Apollo Federation: keys declared with @key
      </text>
      <text x={430} y={364} fill={SLATE} fontFamily={MONO_FONT} fontSize="10.5">
        Composite Schemas: keys inferred from lookup arguments
      </text>
    </svg>
  );
}
