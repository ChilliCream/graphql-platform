import {
  HERO_ACCENT_GRADIENT,
  HERO_DRINKS,
  HERO_HEADLINE,
  HERO_SWIRLS,
} from "@/src/components/home/heroArtwork";
import { Swirl } from "@/src/icons/Swirl";
import { ccBg, ccInk, ccSurface } from "@/src/theme/colors";

type ShareCardProps = {
  /**
   * Optional page name rendered under the headline (e.g. "Pricing"). When
   * omitted the card shows the bare hero artwork, used for the homepage.
   */
  pageTitle?: string;
};

/** `#rrggbb` -> `rgba(r, g, b, a)`, so gradients derive from the same tokens. */
function rgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/**
 * Centered region the divider and page title occupy. Swirls that fall inside it
 * are dropped when a title is shown, so none sit behind the subtitle.
 */
function inTitleZone(left: string, top: string): boolean {
  const l = parseFloat(left);
  const t = parseFloat(top);
  return l >= 15 && l <= 85 && t >= 60 && t <= 80;
}

/**
 * The shared 1200x630 share-card layout used by the marketing OG images. Satori
 * (next/og) supports only flexbox and a subset of CSS, so this stays within
 * those constraints. Reproduces the landing hero (artwork shared via
 * {@link "@/src/components/home/heroArtwork"}): the scattered product drinks
 * behind a centered two-line headline.
 */
export function ShareCard({ pageTitle }: ShareCardProps) {
  return (
    <div
      style={{
        position: "relative",
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        backgroundColor: ccBg,
        backgroundImage: `linear-gradient(160deg, ${ccSurface} 0%, ${ccBg} 70%)`,
        color: ccInk,
        fontFamily: "Josefin Sans",
      }}
    >
      {/* Decorative scatter, behind the headline. */}
      {HERO_DRINKS.map(({ Drink, left, top, cardWidth, aspect, rotate }) => (
        <div
          key={left + top}
          style={{
            position: "absolute",
            left,
            top,
            display: "flex",
            transform: `translate(-50%, -50%) rotate(${rotate})`,
          }}
        >
          <Drink style={{ width: cardWidth, height: cardWidth * aspect }} />
        </div>
      ))}
      {HERO_SWIRLS.filter(
        ({ left, top }) => !pageTitle || !inTitleZone(left, top),
      ).map(({ left, top, cardSize, rotate }) => (
        <div
          key={left + top}
          style={{
            position: "absolute",
            left,
            top,
            display: "flex",
            color: rgba("#62748e", 0.55),
            transform: `translate(-50%, -50%) rotate(${rotate})`,
          }}
        >
          <Swirl style={{ width: cardSize, height: cardSize }} />
        </div>
      ))}

      {/* Headline */}
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          fontSize: "84px",
          fontWeight: 600,
          lineHeight: 1.04,
          letterSpacing: "-1.5px",
        }}
      >
        <div style={{ display: "flex", color: ccInk }}>
          {HERO_HEADLINE.lead}
        </div>
        <div
          style={{
            display: "flex",
            backgroundImage: HERO_ACCENT_GRADIENT,
            backgroundClip: "text",
            WebkitBackgroundClip: "text",
            color: "transparent",
            paddingBottom: "8px",
          }}
        >
          {HERO_HEADLINE.accent}
        </div>
      </div>

      {pageTitle ? (
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            minWidth: "360px",
            marginTop: "36px",
          }}
        >
          {/* Divider with fading edges; stretches to the title width (min 360px). */}
          <div
            style={{
              alignSelf: "stretch",
              height: "2px",
              backgroundImage: `linear-gradient(90deg, ${rgba(ccInk, 0)} 0%, ${rgba(ccInk, 0.5)} 50%, ${rgba(ccInk, 0)} 100%)`,
            }}
          />
          <div
            style={{
              maxWidth: "900px",
              overflow: "hidden",
              textOverflow: "ellipsis",
              whiteSpace: "nowrap",
              marginTop: "28px",
              fontFamily: "Inter",
              fontSize: "40px",
              fontWeight: 700,
              textAlign: "center",
              color: "#ffffff",
            }}
          >
            {pageTitle}
          </div>
        </div>
      ) : null}
    </div>
  );
}
