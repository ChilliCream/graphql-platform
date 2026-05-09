// Grid variant design tokens. Adapted from the reverse-engineered Vercel
// grid system to ChilliCream's dark-navy base. The Grid variant leans on
// strict 1px hairline borders, square corners, and a flat surface palette.
// Use these constants rather than inlining hex values inside grid primitives.

export const GRID_TOKENS = {
  // Surfaces (dark theme adapted to ChilliCream's #0c1322 base)
  bgBase: "#0c1322",
  bgCard: "#0F1828",
  bgInverted: "#040810", // for inverted bands
  bgHover: "#13243A",
  // Borders / dividers
  hairline: "#1F2A3D",
  hairlineStrong: "#2C3A52",
  // Text
  inkPrimary: "#F5F1EA", // headlines / strong text
  inkBody: "rgba(245, 241, 234, 0.78)",
  inkMuted: "rgba(245, 241, 234, 0.55)",
  inkFaint: "rgba(245, 241, 234, 0.32)",
  // Status accents
  success: "#22C55E",
  warning: "#F59E0B",
  danger: "#EF4444",
  // Spacing
  pageMaxWidth: "1280px",
  pageGutter: "clamp(24px, 5vw, 64px)",
  sectionGap: "clamp(96px, 14vw, 160px)",
  cardPadding: "clamp(28px, 3vw, 40px)",
  // Type scale (rough, pages tune as needed)
  heroSize: "clamp(48px, 7vw, 96px)",
  h2Size: "clamp(32px, 4.5vw, 56px)",
  h3Size: "clamp(20px, 2.4vw, 28px)",
  bodySize: "16px",
  eyebrowSize: "11px",
  captionSize: "12px",
} as const;
