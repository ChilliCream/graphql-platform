"use client";

import React, { FC, ReactNode } from "react";
import styled from "styled-components";

import { Link } from "@/components/misc";
import { siteMetadata } from "@/lib/site-config";
import { FONT_FAMILY_HEADING } from "@/style";

const tools = siteMetadata.tools;

interface MenuIngredient {
  readonly label: string;
  readonly href: string;
  // Vertical position (0–100) of the layer this ingredient labels inside the
  // cup viewBox. Drives where the arrow points.
  readonly tapY: number;
}

interface MenuCup {
  readonly key: string;
  readonly title: string;
  readonly tagline: string;
  readonly cupHref: string;
  readonly art: ReactNode;
  readonly ingredients: readonly MenuIngredient[];
}

// All vessel SVGs share viewBox="0 0 240 320" so layer y-coordinates and
// label tap positions stay comparable across the menu.

const HotChocolateCupArt: FC = () => (
  <svg
    viewBox="0 0 240 320"
    width="100%"
    height="100%"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <defs>
      <clipPath id="hc-interior">
        <path d="M 64 76 L 80 286 Q 81 296 91 296 L 149 296 Q 159 296 160 286 L 176 76 Z" />
      </clipPath>
    </defs>

    {/* layered fills — top to bottom */}
    <g clipPath="url(#hc-interior)">
      <rect x="0" y="76" width="240" height="32" fill="#f3e7c8" />
      <rect x="0" y="108" width="240" height="42" fill="#b78657" />
      <rect x="0" y="150" width="240" height="42" fill="#ead9b8" />
      <rect x="0" y="192" width="240" height="48" fill="#6b4226" />
      <rect x="0" y="240" width="240" height="60" fill="#3a2316" />
    </g>

    {/* cup body outline */}
    <path
      d="M 60 76 L 76 290 Q 77 304 91 304 L 149 304 Q 163 304 164 290 L 180 76"
      stroke="var(--cc-ink)"
      strokeWidth="2.5"
      fill="none"
      strokeLinejoin="round"
    />

    {/* lid band */}
    <path d="M 54 56 L 186 56 L 184 78 L 56 78 Z" fill="var(--cc-ink)" />
    {/* lid sip nub */}
    <path d="M 96 36 Q 120 26 144 36 L 144 56 L 96 56 Z" fill="var(--cc-ink)" />
    {/* faint steam */}
    <path
      d="M 104 28 Q 108 18 104 8 M 120 30 Q 124 18 120 6 M 136 28 Q 140 18 136 8"
      stroke="var(--cc-ink)"
      strokeOpacity="0.5"
      strokeWidth="1.5"
      strokeLinecap="round"
      fill="none"
    />
  </svg>
);

const NitroCanArt: FC = () => {
  // Stubby aluminum can (e.g., Starbucks Nitro Cold Brew 9.6oz).
  // Body silhouette: straight cylindrical sides with subtle inward
  // shoulders at the top and bottom that taper down to the lid/base.
  const bodyPath =
    "M 60 122 Q 50 124 50 134 L 50 268 Q 50 278 60 280 L 180 280 " +
    "Q 190 278 190 268 L 190 134 Q 190 124 180 122 Z";
  return (
    <svg
      viewBox="0 0 240 320"
      width="100%"
      height="100%"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
    >
      <defs>
        <clipPath id="nitro-interior">
          <path d={bodyPath} />
        </clipPath>
      </defs>

      {/* layered fill: cream foam crown, then deep cold brew */}
      <g clipPath="url(#nitro-interior)">
        <rect x="0" y="122" width="240" height="32" fill="#f3e7c8" />
        <rect x="0" y="154" width="240" height="40" fill="#1f2a3a" />
        <rect x="0" y="194" width="240" height="44" fill="#0e1726" />
        <rect x="0" y="238" width="240" height="44" fill="#070b14" />
        {/* nitrogen cascade */}
        <circle cx="74" cy="194" r="2.6" fill="#f3e7c8" opacity="0.7" />
        <circle cx="96" cy="218" r="2" fill="#f3e7c8" opacity="0.6" />
        <circle cx="120" cy="202" r="2.4" fill="#f3e7c8" opacity="0.7" />
        <circle cx="148" cy="226" r="1.8" fill="#f3e7c8" opacity="0.55" />
        <circle cx="166" cy="200" r="2.2" fill="#f3e7c8" opacity="0.65" />
        <circle cx="86" cy="248" r="1.8" fill="#f3e7c8" opacity="0.5" />
        <circle cx="118" cy="262" r="2.4" fill="#f3e7c8" opacity="0.6" />
        <circle cx="158" cy="252" r="1.6" fill="#f3e7c8" opacity="0.45" />
      </g>

      {/* lid: narrower than body, sitting above the top shoulder */}
      <rect
        x="60"
        y="104"
        width="120"
        height="16"
        rx="2"
        fill="var(--cc-ink)"
      />
      {/* pull tab cut into the lid */}
      <ellipse
        cx="120"
        cy="112"
        rx="20"
        ry="4.5"
        stroke="var(--cc-background-color, #0c1322)"
        strokeWidth="1.4"
        fill="none"
      />
      <circle
        cx="120"
        cy="112"
        r="1.8"
        fill="var(--cc-background-color, #0c1322)"
      />

      {/* body outline — straight sides with shoulder curves */}
      <path
        d={bodyPath}
        stroke="var(--cc-ink)"
        strokeWidth="2.5"
        fill="none"
        strokeLinejoin="round"
      />

      {/* base ring (mirrors the lid below the bottom shoulder) */}
      <rect
        x="60"
        y="282"
        width="120"
        height="14"
        rx="2"
        stroke="var(--cc-ink)"
        strokeWidth="1.5"
        fill="none"
      />
    </svg>
  );
};

const MochaCupArt: FC = () => (
  <svg
    viewBox="0 0 240 320"
    width="100%"
    height="100%"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <defs>
      <clipPath id="mocha-interior">
        <path d="M 70 90 L 70 254 Q 70 274 90 274 L 150 274 Q 170 274 170 254 L 170 90 Z" />
      </clipPath>
    </defs>

    {/* layers in mug */}
    <g clipPath="url(#mocha-interior)">
      <rect x="0" y="90" width="240" height="38" fill="#f3e7c8" />
      <rect x="0" y="128" width="240" height="46" fill="#d8b88a" />
      <rect x="0" y="174" width="240" height="50" fill="#7a4a2d" />
      <rect x="0" y="224" width="240" height="60" fill="#2a1610" />
    </g>

    {/* mug body */}
    <path
      d="M 70 90 L 70 256 Q 70 280 94 280 L 146 280 Q 170 280 170 256 L 170 90"
      stroke="var(--cc-ink)"
      strokeWidth="2.5"
      fill="none"
      strokeLinejoin="round"
    />

    {/* mug rim ellipse */}
    <ellipse
      cx="120"
      cy="90"
      rx="50"
      ry="10"
      stroke="var(--cc-ink)"
      strokeWidth="2.5"
      fill="var(--cc-background-color, #0c1322)"
    />
    {/* coffee surface */}
    <ellipse cx="120" cy="90" rx="44" ry="7" fill="#f3e7c8" />

    {/* whipped cream peak */}
    <path
      d="M 96 90 Q 100 60 116 76 Q 122 56 138 76 Q 152 64 152 92"
      fill="#f5f1ea"
      stroke="var(--cc-ink)"
      strokeWidth="1.6"
      strokeLinejoin="round"
    />

    {/* handle */}
    <path
      d="M 170 130 Q 210 130 210 175 Q 210 220 170 220"
      stroke="var(--cc-ink)"
      strokeWidth="2.5"
      fill="none"
      strokeLinecap="round"
    />
    <path
      d="M 170 144 Q 196 144 196 175 Q 196 206 170 206"
      stroke="var(--cc-ink)"
      strokeOpacity="0.5"
      strokeWidth="1.2"
      fill="none"
    />

    {/* cocoa dust */}
    <circle cx="110" cy="86" r="1.2" fill="#3a2316" />
    <circle cx="124" cy="84" r="1" fill="#3a2316" />
    <circle cx="134" cy="88" r="1.4" fill="#3a2316" />
  </svg>
);

const StrawberryShakeArt: FC = () => (
  <svg
    viewBox="0 0 240 320"
    width="100%"
    height="100%"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <defs>
      <clipPath id="ss-interior">
        <path d="M 78 88 L 90 296 Q 91 304 100 304 L 140 304 Q 149 304 150 296 L 162 88 Z" />
      </clipPath>
    </defs>

    {/* shake layers */}
    <g clipPath="url(#ss-interior)">
      <rect x="0" y="88" width="240" height="44" fill="#fbe4ec" />
      <rect x="0" y="132" width="240" height="56" fill="#f7a9c1" />
      <rect x="0" y="188" width="240" height="58" fill="#e96996" />
      <rect x="0" y="246" width="240" height="60" fill="#b53b6a" />
      {/* strawberry seeds */}
      <circle cx="100" cy="266" r="1.6" fill="#3a0a18" />
      <circle cx="118" cy="276" r="1.4" fill="#3a0a18" />
      <circle cx="132" cy="262" r="1.6" fill="#3a0a18" />
      <circle cx="142" cy="282" r="1.4" fill="#3a0a18" />
    </g>

    {/* glass outline */}
    <path
      d="M 74 88 L 86 298 Q 87 312 102 312 L 138 312 Q 153 312 154 298 L 166 88"
      stroke="var(--cc-ink)"
      strokeWidth="2.5"
      fill="none"
      strokeLinejoin="round"
    />

    {/* whipped cream dome */}
    <path
      d="M 78 88 Q 86 60 104 70 Q 116 46 134 68 Q 156 60 162 90"
      fill="#f5f1ea"
      stroke="var(--cc-ink)"
      strokeWidth="1.8"
      strokeLinejoin="round"
    />

    {/* cherry */}
    <circle
      cx="138"
      cy="42"
      r="9"
      fill="#d62b4a"
      stroke="var(--cc-ink)"
      strokeWidth="1.4"
    />
    <path
      d="M 138 33 Q 148 18 162 16"
      stroke="#3a5a1a"
      strokeWidth="1.8"
      strokeLinecap="round"
      fill="none"
    />

    {/* straw */}
    <rect
      x="98"
      y="20"
      width="8"
      height="78"
      transform="rotate(-12 102 60)"
      fill="#f5f1ea"
      stroke="var(--cc-ink)"
      strokeWidth="1.4"
    />
    <line
      x1="102"
      y1="38"
      x2="98"
      y2="52"
      transform="rotate(-12 102 60)"
      stroke="var(--cc-ink)"
      strokeOpacity="0.4"
      strokeWidth="1"
    />
  </svg>
);

const MENU_CUPS: readonly MenuCup[] = [
  {
    key: "hot-chocolate",
    title: "Hot Chocolate",
    tagline: "The GraphQL server for .NET",
    cupHref: "/products/hotchocolate",
    art: <HotChocolateCupArt />,
    ingredients: [
      // tapY values map to layer centers in the 320-unit viewBox.
      {
        label: "Semantic Introspection",
        href: "/blog/2026/04/22/semantic-introspection",
        tapY: 29,
      },
      {
        label: "Type System",
        href: "/docs/hotchocolate/v16/building-a-schema",
        tapY: 41,
      },
      {
        label: "Resolvers",
        href: "/docs/hotchocolate/v16/resolvers-and-data",
        tapY: 53,
      },
      { label: "Federation", href: "/docs/hotchocolate/v16/fusion", tapY: 67 },
      { label: "Documentation", href: "/docs/hotchocolate/v16", tapY: 84 },
    ],
  },
  {
    key: "nitro",
    title: "Nitro",
    tagline: "GraphQL IDE & API cockpit. Cold-poured, on tap.",
    cupHref: "/products/nitro",
    art: <NitroCanArt />,
    ingredients: [
      { label: "Launch Nitro", href: tools.nitro, tapY: 41 },
      {
        label: "Observability",
        href: "/products/nitro/observability",
        tapY: 53,
      },
      { label: "Agents", href: "/products/nitro/agents", tapY: 68 },
      { label: "Documentation", href: "/docs/nitro", tapY: 82 },
    ],
  },
  {
    key: "mocha",
    title: "Mocha",
    tagline: "Async messaging for .NET",
    cupHref: "/docs/mocha/v16",
    art: <MochaCupArt />,
    ingredients: [
      { label: "Documentation", href: "/docs/mocha/v16", tapY: 30 },
      { label: "Transports", href: "/docs/mocha/v16/transports", tapY: 47 },
      { label: "Sagas", href: "/docs/mocha/v16/sagas", tapY: 62 },
      { label: "Reliability", href: "/docs/mocha/v16/reliability", tapY: 80 },
    ],
  },
  {
    key: "strawberry-shake",
    title: "Strawberry Shake",
    tagline: "The .NET GraphQL client",
    cupHref: "/products/strawberryshake",
    art: <StrawberryShakeArt />,
    ingredients: [
      { label: "Documentation", href: "/docs/strawberryshake/v16", tapY: 32 },
      {
        label: "Get Started",
        href: "/docs/strawberryshake/v16/get-started",
        tapY: 48,
      },
      { label: "Caching", href: "/docs/strawberryshake/v16/caching", tapY: 66 },
      {
        label: "Subscriptions",
        href: "/docs/strawberryshake/v16/subscriptions",
        tapY: 84,
      },
    ],
  },
];

const MenuArrow: FC<{ length?: number }> = ({ length = 96 }) => {
  const h = 14;
  // Curved arrow that points left, with the arrowhead at x=4 and the tail at x=length-2.
  return (
    <svg
      width={length}
      height={h}
      viewBox={`0 0 ${length} ${h}`}
      fill="none"
      aria-hidden="true"
    >
      <path
        d={`M ${length - 2} 7 Q ${length / 2} -2 4 8`}
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
      />
      <path
        d="M 4 8 L 11 4 M 4 8 L 10 13"
        stroke="currentColor"
        strokeWidth="1"
        strokeLinecap="round"
      />
    </svg>
  );
};

interface MenuPopoutProps {
  readonly onItemClick: () => void;
}

export const MenuPopout: FC<MenuPopoutProps> = ({ onItemClick }) => (
  <MenuPopoutRoot>
    <MenuPopoutHeader>
      <MenuPopoutEyebrow>The Menu</MenuPopoutEyebrow>
      <MenuPopoutSubtitle>
        Pick your blend. Every cup is a product, every layer a feature.
      </MenuPopoutSubtitle>
    </MenuPopoutHeader>

    <MenuPopoutStack>
      {MENU_CUPS.map((cup) => (
        <MenuCupRow key={cup.key}>
          <MenuCupHeader>
            <MenuCupHeading>
              <Link to={cup.cupHref} onClick={onItemClick}>
                {cup.title}
              </Link>
            </MenuCupHeading>
            <MenuCupTagline>{cup.tagline}</MenuCupTagline>
          </MenuCupHeader>
          <MenuCupBody>
            <MenuCupArtCell>
              <Link
                to={cup.cupHref}
                onClick={onItemClick}
                aria-label={cup.title}
              >
                {cup.art}
              </Link>
            </MenuCupArtCell>
            <MenuCupIngredients>
              {cup.ingredients.map((ingredient) => (
                <MenuIngredientRow
                  key={ingredient.label}
                  style={{ top: `${ingredient.tapY}%` }}
                >
                  <MenuArrow />
                  <MenuIngredientLink
                    to={ingredient.href}
                    onClick={onItemClick}
                    prefetch={false}
                  >
                    {ingredient.label}
                  </MenuIngredientLink>
                </MenuIngredientRow>
              ))}
            </MenuCupIngredients>
          </MenuCupBody>
        </MenuCupRow>
      ))}
    </MenuPopoutStack>
  </MenuPopoutRoot>
);

const MenuPopoutRoot = styled.div`
  --cc-ink: #f5f1ea;
  --cc-ink-muted: rgba(245, 241, 234, 0.62);
  --cc-ink-faint: rgba(245, 241, 234, 0.16);

  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 8px;
  padding: 20px 16px;
  color: var(--cc-ink);
  width: 100%;

  @media only screen and (min-width: 992px) {
    gap: 4px;
    padding: 24px 28px 24px;
    overflow-y: auto;
    scrollbar-width: thin;
    scrollbar-color: var(--cc-ink-faint) transparent;
  }

  &::-webkit-scrollbar {
    width: 8px;
  }

  &::-webkit-scrollbar-thumb {
    background-color: var(--cc-ink-faint);
    border-radius: 4px;
  }
`;

const MenuPopoutHeader = styled.div`
  display: none;

  @media only screen and (min-width: 992px) {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 4px;
    padding-bottom: 8px;
    text-align: center;
  }
`;

const MenuPopoutEyebrow = styled.div`
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.7rem;
  font-weight: 500;
  letter-spacing: 0.24em;
  text-transform: uppercase;
  color: var(--cc-ink);
`;

const MenuPopoutSubtitle = styled.div`
  font-size: 0.78rem;
  color: var(--cc-ink-muted);
`;

const MenuPopoutStack = styled.div`
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex-shrink: 0;
`;

const MenuCupRow = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 20px 4px 16px;
  border-top: 1px solid var(--cc-ink-faint);
  flex-shrink: 0;

  &:first-child {
    border-top: none;
    padding-top: 8px;
  }

  @media only screen and (min-width: 992px) {
    padding: 24px 8px 20px;
  }
`;

const MenuCupHeader = styled.div`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 4px;

  @media only screen and (min-width: 992px) {
    flex-direction: row;
    align-items: baseline;
    gap: 12px;
  }
`;

const MenuCupHeading = styled.div`
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 0.95rem;
  font-weight: 600;
  letter-spacing: 0.22em;
  text-transform: uppercase;

  > a {
    color: var(--cc-ink);
    text-decoration: none;
    transition: color 0.2s ease;
  }

  > a:hover {
    color: #ffffff;
  }
`;

const MenuCupTagline = styled.div`
  font-size: 0.78rem;
  color: var(--cc-ink-muted);
`;

const MenuCupBody = styled.div`
  position: relative;
  display: grid;
  grid-template-columns: 200px 1fr;
  gap: 16px;
  align-items: stretch;
  height: 240px;
  flex-shrink: 0;

  @media only screen and (min-width: 992px) {
    grid-template-columns: 220px 1fr;
    gap: 24px;
    height: 260px;
  }
`;

const MenuCupArtCell = styled.div`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;

  > a {
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    height: 100%;
  }

  svg {
    width: auto;
    height: 100%;
    max-width: 100%;
  }
`;

const MenuCupIngredients = styled.div`
  position: relative;
  width: 100%;
  height: 100%;
`;

const MenuIngredientRow = styled.div`
  position: absolute;
  left: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  transform: translateY(-50%);
  color: var(--cc-ink-muted);

  > svg {
    flex: 0 0 auto;
    color: var(--cc-ink-muted);
    opacity: 0.75;
    transition: color 0.2s ease, opacity 0.2s ease;
  }

  &:hover > svg {
    color: var(--cc-ink);
    opacity: 1;
  }
`;

const MenuIngredientLink = styled(Link)`
  font-size: 0.82rem;
  font-weight: 500;
  line-height: 1.3;
  color: var(--cc-ink-muted);
  text-decoration: none;
  white-space: nowrap;
  transition: color 0.2s ease;

  &:hover {
    color: var(--cc-ink);
  }
`;
