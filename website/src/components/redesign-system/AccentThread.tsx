"use client";

import React from "react";
import styled from "styled-components";

export type PageAccent =
  | "pricing"
  | "enterprise"
  | "observability"
  | "agents"
  | "customers"
  | "templates"
  | "integrations"
  | "solutions";

export interface AccentTokens {
  /** oklch base */
  primary: string;
  /** rgba 8-12%, used for accent band background */
  soft: string;
  /** rgba ~32%, used for accent borders/rules */
  line: string;
  /** linear-gradient or single oklch */
  gradient: string;
  /** radial-gradient / rgba glow tint */
  glow: string;
}

export const PAGE_ACCENTS: Record<PageAccent, AccentTokens> = {
  pricing: {
    primary: "oklch(0.78 0.13 250)",
    soft: "rgba(140, 160, 240, 0.10)",
    line: "rgba(140, 160, 240, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.13 250), oklch(0.74 0.16 290))",
    glow: "rgba(140, 160, 240, 0.18)",
  },
  enterprise: {
    primary: "oklch(0.72 0.14 230)",
    soft: "rgba(108, 156, 220, 0.10)",
    line: "rgba(108, 156, 220, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.72 0.14 230), oklch(0.78 0.10 220))",
    glow: "rgba(108, 156, 220, 0.18)",
  },
  observability: {
    primary: "oklch(0.78 0.14 200)",
    soft: "rgba(96, 200, 220, 0.10)",
    line: "rgba(96, 200, 220, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.14 200), oklch(0.82 0.12 180))",
    glow: "rgba(96, 200, 220, 0.20)",
  },
  agents: {
    primary: "oklch(0.78 0.16 70)",
    soft: "rgba(247, 186, 100, 0.10)",
    line: "rgba(247, 186, 100, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.16 70), oklch(0.74 0.18 40))",
    glow: "rgba(247, 186, 100, 0.20)",
  },
  customers: {
    primary: "oklch(0.74 0.10 70)",
    soft: "rgba(220, 200, 160, 0.10)",
    line: "rgba(220, 200, 160, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.74 0.10 70), oklch(0.72 0.12 40))",
    glow: "rgba(220, 200, 160, 0.16)",
  },
  templates: {
    primary: "oklch(0.78 0.14 60)",
    soft: "rgba(220, 180, 120, 0.10)",
    line: "rgba(220, 180, 120, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.78 0.14 60), oklch(0.76 0.16 30))",
    glow: "rgba(220, 180, 120, 0.18)",
  },
  integrations: {
    primary: "oklch(0.74 0.16 150)",
    soft: "rgba(80, 200, 140, 0.10)",
    line: "rgba(80, 200, 140, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.74 0.16 150), oklch(0.78 0.14 170))",
    glow: "rgba(80, 200, 140, 0.18)",
  },
  solutions: {
    primary: "oklch(0.74 0.16 320)",
    soft: "rgba(220, 140, 220, 0.10)",
    line: "rgba(220, 140, 220, 0.32)",
    gradient:
      "linear-gradient(120deg, oklch(0.74 0.16 320), oklch(0.72 0.18 350))",
    glow: "rgba(220, 140, 220, 0.18)",
  },
};

export interface AccentVarsCSS extends React.CSSProperties {
  "--cc-accent": string;
  "--cc-accent-soft": string;
  "--cc-accent-line": string;
  "--cc-accent-gradient": string;
  "--cc-accent-glow": string;
}

const resolveTokens = (
  page: PageAccent,
  override?: Partial<AccentTokens>
): AccentTokens => ({
  ...PAGE_ACCENTS[page],
  ...(override ?? {}),
});

/**
 * Returns the accent custom properties for a given page as a JS object,
 * suitable for use as inline style on elements that cannot live inside
 * an `AccentThread` wrapper.
 */
export const useAccentVars = (
  page: PageAccent,
  override?: Partial<AccentTokens>
): AccentVarsCSS => {
  const tokens = resolveTokens(page, override);
  return {
    "--cc-accent": tokens.primary,
    "--cc-accent-soft": tokens.soft,
    "--cc-accent-line": tokens.line,
    "--cc-accent-gradient": tokens.gradient,
    "--cc-accent-glow": tokens.glow,
  };
};

const Thread = styled.div`
  display: contents;
`;

export interface AccentThreadProps {
  page: PageAccent;
  children: React.ReactNode;
  override?: Partial<AccentTokens>;
}

/**
 * Exposes the per-page accent tokens as CSS custom properties on its subtree:
 * `--cc-accent`, `--cc-accent-soft`, `--cc-accent-line`,
 * `--cc-accent-gradient`, `--cc-accent-glow`.
 *
 * Custom properties cascade through the DOM, so `display: contents` is used
 * to avoid introducing a layout box around the page content.
 */
export const AccentThread: React.FC<AccentThreadProps> = ({
  page,
  children,
  override,
}) => {
  const style = useAccentVars(page, override);
  return <Thread style={style}>{children}</Thread>;
};
