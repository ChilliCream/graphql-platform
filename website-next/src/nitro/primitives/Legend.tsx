/**
 * Legend — the little pill/dot series legend that sits in a panel header (matches the
 * Nitro monitoring reference: a filled dot for active series, a hollow ring for toggled-
 * off ones, a square for the error series). Purely presentational.
 */
import type { CSSProperties } from "react";
import { token } from "../lib/tokens";

export interface LegendItem {
  label: string;
  color: string;
  shape?: "dot" | "ring" | "square";
  /** rendered greyed-out (a toggled-off series in the reference) */
  muted?: boolean;
}

function Swatch({ item }: { item: LegendItem }) {
  const c = item.muted ? token.textSecondary : item.color;
  const shape = item.shape ?? "dot";
  if (shape === "square") {
    return (
      <span
        style={{
          width: 12,
          height: 12,
          borderRadius: 3,
          background: c,
          display: "inline-block",
        }}
      />
    );
  }
  return (
    <span
      style={{
        width: 11,
        height: 11,
        borderRadius: "50%",
        display: "inline-block",
        background: shape === "ring" ? "transparent" : c,
        border: `2px solid ${c}`,
        boxSizing: "border-box",
      }}
    />
  );
}

export function Legend({
  items,
  style,
}: {
  items: LegendItem[];
  style?: CSSProperties;
}) {
  return (
    <div
      style={{
        display: "flex",
        gap: 12,
        alignItems: "center",
        flexWrap: "wrap",
        ...style,
      }}
    >
      {items.map((it) => (
        <span
          key={it.label}
          style={{ display: "flex", alignItems: "center", gap: 6 }}
        >
          <Swatch item={it} />
          <span
            style={{
              fontSize: 11,
              color: it.muted ? token.textSecondary : token.text,
            }}
          >
            {it.label}
          </span>
        </span>
      ))}
    </div>
  );
}
