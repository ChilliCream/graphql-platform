import { Swirl } from "@/src/illustrations/Swirl";
import { PRODUCT_DRINKS } from "@/src/og/productDrinks";
import { ccAccent, ccBg, ccDarkSurface, ccInk } from "@/src/theme/colors";

/** `#rrggbb` -> `rgba(r, g, b, a)`, so tints derive from the same tokens. */
function rgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

type DocsShareCardProps = {
  /** Small uppercase accent line above the title. */
  eyebrow: string;
  /** Headline, large text lower-left. */
  title: string;
  /** Product slug; renders the matching drink icon in the top-right. */
  productSlug?: string;
};

/** Top-right drink icon is sized to this height; width follows the aspect. */
const ICON_HEIGHT = 280;

interface SwirlMark {
  readonly left: string;
  readonly top: string;
  readonly size: number;
  readonly rotate: string;
}

// Swirls scattered through the empty upper band to fill the space. Kept above
// the title's region (which sits lower-left and can span the full width).
const SCATTER_SWIRLS: readonly SwirlMark[] = [
  { left: "12%", top: "20%", size: 24, rotate: "-12deg" },
  { left: "26%", top: "38%", size: 18, rotate: "16deg" },
  { left: "41%", top: "17%", size: 22, rotate: "-8deg" },
  { left: "54%", top: "42%", size: 18, rotate: "20deg" },
  { left: "61%", top: "14%", size: 20, rotate: "10deg" },
  { left: "17%", top: "46%", size: 18, rotate: "-16deg" },
  { left: "48%", top: "29%", size: 16, rotate: "8deg" },
  { left: "68%", top: "47%", size: 18, rotate: "-10deg" },
];

/**
 * The 1200x630 share-card layout used by the per-doc OG images. Satori
 * (next/og) supports only flexbox and a subset of CSS, so this stays within
 * those constraints. Shares the marketing card background and shows the
 * product's drink icon in the top-right.
 */
export function DocsShareCard({
  eyebrow,
  title,
  productSlug,
}: DocsShareCardProps) {
  const drink = productSlug ? PRODUCT_DRINKS[productSlug] : undefined;
  const iconWidth = drink ? ICON_HEIGHT / drink.aspect : 0;
  // Icon sits at top:72/right:72; this is its center on the 1200x630 frame.
  const iconCenterX = 1200 - 72 - iconWidth / 2;
  const iconCenterY = 72 + ICON_HEIGHT / 2;
  const glowSize = ICON_HEIGHT * 2;

  return (
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        justifyContent: "flex-end",
        padding: "72px",
        backgroundColor: ccBg,
        backgroundImage: ccDarkSurface,
        color: ccInk,
        fontFamily: "Inter",
      }}
    >
      {/* Decorative swirl scatter, behind the icon and content. */}
      {SCATTER_SWIRLS.map(({ left, top, size, rotate }) => (
        <div
          key={left + top}
          style={{
            position: "absolute",
            left,
            top,
            display: "flex",
            color: rgba("#62748e", 0.5),
            transform: `translate(-50%, -50%) rotate(${rotate})`,
          }}
        >
          <Swirl style={{ width: size, height: size }} />
        </div>
      ))}
      {/* Soft glow highlighting the product, centered on the icon. */}
      {drink ? (
        <div
          style={{
            position: "absolute",
            left: `${iconCenterX}px`,
            top: `${iconCenterY}px`,
            width: glowSize,
            height: glowSize,
            borderRadius: "50%",
            transform: "translate(-50%, -50%)",
            backgroundImage: `radial-gradient(circle, ${rgba(ccAccent, 0.4)} 0%, ${rgba(ccAccent, 0.14)} 34%, ${rgba(ccAccent, 0)} 66%)`,
          }}
        />
      ) : null}

      {drink ? (
        <div
          style={{
            position: "absolute",
            top: "72px",
            right: "72px",
            display: "flex",
            transform: "rotate(8deg)",
          }}
        >
          <drink.Icon style={{ width: iconWidth, height: ICON_HEIGHT }} />
        </div>
      ) : null}

      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: "8px",
        }}
      >
        <div
          style={{
            display: "flex",
            fontSize: "28px",
            fontWeight: 700,
            letterSpacing: "2px",
            textTransform: "uppercase",
            color: ccInk,
          }}
        >
          {eyebrow}
        </div>
        <div
          style={{
            display: "-webkit-box",
            WebkitBoxOrient: "vertical",
            WebkitLineClamp: 2,
            overflow: "hidden",
            textOverflow: "ellipsis",
            // Reserve two lines so the content always starts at the same height,
            // whether the title is one line or two.
            minHeight: "150px",
            fontSize: "68px",
            fontWeight: 700,
            lineHeight: 1.1,
            color: ccInk,
          }}
        >
          {title}
        </div>
      </div>
    </div>
  );
}
