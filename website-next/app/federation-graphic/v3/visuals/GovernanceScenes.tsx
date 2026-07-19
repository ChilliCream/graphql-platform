/**
 * Section scenes for schema governance: a composition check failing before
 * deploy, and field ownership migrating between teams without client impact.
 * CORAL doubles as the error hue (matches the site's status-firing token).
 */

import {
  AMBER,
  CARD_BG,
  CARD_BORDER,
  CODE,
  CORAL,
  CYAN,
  INK,
  MONO_FONT,
  SLATE,
  TEAL,
} from "../palette";

export function CheckFail() {
  return (
    <svg aria-hidden="true" className="h-auto w-full" viewBox="0 0 900 300">
      {/* Two teams declare the same field with different types. */}
      <g>
        <rect
          x={30}
          y={40}
          width={260}
          height={84}
          rx={12}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <circle cx={52} cy={62} r={4} fill={CYAN} />
        <text
          x={66}
          y={66}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.18em"
        >
          CATALOG
        </text>
        <text x={50} y={100} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          price: Money!
        </text>
      </g>
      <g>
        <rect
          x={30}
          y={160}
          width={260}
          height={84}
          rx={12}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <circle cx={52} cy={182} r={4} fill={AMBER} />
        <text
          x={66}
          y={186}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.18em"
        >
          PRICING
        </text>
        <text x={50} y={220} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          price: Float!
        </text>
      </g>

      <path
        d="M 290 82 C 360 82, 380 142, 450 142"
        fill="none"
        stroke={CYAN}
        strokeWidth="2"
        strokeOpacity="0.6"
      />
      <path
        d="M 290 202 C 360 202, 380 142, 450 142"
        fill="none"
        stroke={AMBER}
        strokeWidth="2"
        strokeOpacity="0.6"
      />

      {/* The build fails; clients never see it. */}
      <g>
        <rect
          x={450}
          y={70}
          width={420}
          height={144}
          rx={14}
          fill={CARD_BG}
          stroke={CORAL}
          strokeOpacity="0.6"
        />
        <circle
          cx={476}
          cy={98}
          r={8}
          fill="none"
          stroke={CORAL}
          strokeWidth="2"
        />
        <line
          x1={472}
          y1={94}
          x2={480}
          y2={102}
          stroke={CORAL}
          strokeWidth="2"
          strokeLinecap="round"
        />
        <line
          x1={480}
          y1={94}
          x2={472}
          y2={102}
          stroke={CORAL}
          strokeWidth="2"
          strokeLinecap="round"
        />
        <text
          x={494}
          y={103}
          fill={CORAL}
          fontFamily={MONO_FONT}
          fontSize="12.5"
          fontWeight="600"
        >
          COMPOSITION FAILED
        </text>
        <text x={470} y={136} fill={INK} fontFamily={MONO_FONT} fontSize="12">
          OUTPUT_FIELD_TYPES_NOT_MERGEABLE
        </text>
        <text
          x={470}
          y={158}
          fill={CODE}
          fontFamily={MONO_FONT}
          fontSize="11.5"
        >
          Product.price — Money! (catalog) vs Float! (pricing)
        </text>
        <text
          x={470}
          y={192}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10.5"
          opacity="0.85"
        >
          caught in the build · nothing deployed · no client saw it
        </text>
      </g>
    </svg>
  );
}

export function OwnershipMove() {
  return (
    <svg aria-hidden="true" className="h-auto w-full" viewBox="0 0 900 300">
      {/* price moves from catalog to pricing. */}
      <g>
        <rect
          x={30}
          y={40}
          width={260}
          height={104}
          rx={12}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <circle cx={52} cy={62} r={4} fill={CYAN} />
        <text
          x={66}
          y={66}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.18em"
        >
          CATALOG
        </text>
        <text x={50} y={100} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          name: String!
        </text>
        <text
          x={50}
          y={124}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="13"
          opacity="0.55"
          textDecoration="line-through"
        >
          price: Money!
        </text>
      </g>
      <g>
        <rect
          x={30}
          y={176}
          width={260}
          height={84}
          rx={12}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <circle cx={52} cy={198} r={4} fill={AMBER} />
        <text
          x={66}
          y={202}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.18em"
        >
          PRICING
        </text>
        <text x={50} y={236} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          price: Money!
        </text>
        <text
          x={220}
          y={236}
          fill={AMBER}
          fontFamily={MONO_FONT}
          fontSize="10.5"
        >
          new owner
        </text>
      </g>
      <path
        d="M 200 124 C 240 140, 240 160, 200 176"
        fill="none"
        stroke={AMBER}
        strokeWidth="1.5"
        strokeDasharray="3 4"
        strokeOpacity="0.8"
        markerEnd="url(#own-arrow)"
      />
      <defs>
        <marker
          id="own-arrow"
          viewBox="0 0 8 8"
          refX="6"
          refY="4"
          markerWidth="7"
          markerHeight="7"
          orient="auto"
        >
          <path d="M 0 0 L 8 4 L 0 8 z" fill={AMBER} />
        </marker>
      </defs>

      {/* The composed type is unchanged. */}
      <g>
        <rect
          x={470}
          y={64}
          width={310}
          height={172}
          rx={14}
          fill={CARD_BG}
          stroke={CARD_BORDER}
        />
        <rect
          x={463}
          y={57}
          width={324}
          height={186}
          rx={20}
          fill="none"
          stroke={TEAL}
          strokeOpacity="0.35"
          strokeWidth="2"
        />
        <text
          x={490}
          y={92}
          fill={INK}
          fontFamily={MONO_FONT}
          fontSize="14"
          fontWeight="600"
        >
          Product
        </text>
        <text
          x={760}
          y={92}
          textAnchor="end"
          fill={TEAL}
          fontFamily={MONO_FONT}
          fontSize="10"
          fontWeight="600"
          letterSpacing="0.22em"
        >
          UNCHANGED
        </text>
        <line x1={470} x2={780} y1={104} y2={104} stroke={CARD_BORDER} />
        <text x={490} y={130} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          id: ID!
        </text>
        <text x={490} y={154} fill={CODE} fontFamily={MONO_FONT} fontSize="13">
          name: String!
        </text>
        <g>
          <text
            x={490}
            y={178}
            fill={CODE}
            fontFamily={MONO_FONT}
            fontSize="13"
          >
            price: Money!
          </text>
          <circle cx={732} cy={174} r={3.5} fill={AMBER} />
        </g>
        <text x={490} y={202} fill={SLATE} fontFamily={MONO_FONT} fontSize="13">
          legacySku: String @deprecated
        </text>
        <text
          x={490}
          y={224}
          fill={SLATE}
          fontFamily={MONO_FONT}
          fontSize="10.5"
          opacity="0.85"
        >
          clients query the same field · a different team answers
        </text>
      </g>

      {/* Only the owner chip changed color. */}
      <path
        d="M 290 218 C 380 218, 390 174, 470 174"
        fill="none"
        stroke={AMBER}
        strokeWidth="2"
        strokeOpacity="0.6"
      />
    </svg>
  );
}
