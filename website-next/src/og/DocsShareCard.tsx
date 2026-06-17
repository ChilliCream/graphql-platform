import { ccAccent, ccBg, ccInk, ccSurface } from "@/src/theme/colors";

/** `#rrggbb` -> `rgba(r, g, b, a)`, so gradients derive from the same tokens. */
function rgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

type DocsShareCardProps = {
  /** Brand badge text in the top-right box. */
  badge: string;
  /** Small uppercase accent line above the title. */
  eyebrow: string;
  /** Headline, large text lower-left. */
  title: string;
};

/**
 * The 1200x630 share-card layout used by the per-doc OG images. Satori
 * (next/og) supports only flexbox and a subset of CSS, so this stays within
 * those constraints.
 */
export function DocsShareCard({ badge, eyebrow, title }: DocsShareCardProps) {
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
        backgroundImage:
          `radial-gradient(1000px 600px at 85% 0%, ${rgba(ccAccent, 0.18)}, ${rgba(ccBg, 0)}), ` +
          `linear-gradient(135deg, ${ccSurface} 0%, ${ccBg} 60%)`,
        color: ccInk,
        fontFamily: "Inter",
      }}
    >
      <div
        style={{
          position: "absolute",
          top: "72px",
          right: "72px",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          padding: "16px 28px",
          borderRadius: "16px",
          border: `2px solid ${ccAccent}`,
          backgroundColor: rgba(ccAccent, 0.08),
          fontSize: "30px",
          fontWeight: 700,
          color: ccInk,
        }}
      >
        {badge}
      </div>

      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: "20px",
          maxWidth: "900px",
        }}
      >
        <div
          style={{
            display: "flex",
            fontSize: "28px",
            fontWeight: 700,
            letterSpacing: "2px",
            textTransform: "uppercase",
            color: ccAccent,
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
