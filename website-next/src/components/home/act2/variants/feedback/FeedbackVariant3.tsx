/**
 * Agentic coding, variant 3: the "field-in-use" callout (static Nitro card).
 *
 * A small, focused registry-feedback widget: the coordinate `Product.price` with
 * "in use by 3 clients" and three tiny client chips (Web, iOS, Admin). This is the
 * usage signal a coding agent reads from the schema registry before it touches a
 * field, so a rename or removal is a deliberate, informed change rather than a
 * blind one.
 *
 * Styled as one cropped slice of the Nitro / Banana Cake Pop UI (GitHub-dark): a
 * self-contained card on nitro.bg with a 1px nitro.border hairline and 6px
 * corners, Inter for chrome and nitro.mono for the coordinate / data, ~11-12px
 * text. No tabs, toolbars, or app navigation: just the single callout.
 *
 * Static by design: the settled final frame, no motion package, no hooks, no
 * clocks, no "use client". SVG ids are prefixed "feedback-v3-". Colors come from
 * the shared Nitro palette via inline style, not the site cc-* tokens. Believable
 * EShops sample data.
 */
import type { CSSProperties } from "react";

import { nitro } from "@/src/components/home/act2/variants/nitroTheme";

interface FeedbackVariant3Props {
  readonly className?: string;
}

interface Client {
  readonly id: string;
  readonly label: string;
}

/** The three published EShops clients reading `Product.price`. */
const CLIENTS: readonly Client[] = [
  { id: "web", label: "Web" },
  { id: "ios", label: "iOS" },
  { id: "admin", label: "Admin" },
];

const MONO: CSSProperties = {
  fontFamily: nitro.mono,
  fontVariantNumeric: "tabular-nums",
};

/** Small "people" glyph marking the usage row. */
function UsersMark() {
  return (
    <svg
      width={13}
      height={13}
      viewBox="0 0 16 16"
      aria-hidden="true"
      fill="none"
      stroke={nitro.warning}
      strokeWidth={1.3}
      strokeLinecap="round"
      strokeLinejoin="round"
      style={{ flex: "none" }}
    >
      <circle cx="6" cy="5" r="2.4" />
      <path d="M1.8 13.2c0-2.3 1.9-3.8 4.2-3.8s4.2 1.5 4.2 3.8" />
      <path d="M10.6 3.1a2.2 2.2 0 0 1 0 4.1M11.4 9.6c1.8.3 3 1.6 3 3.6" />
    </svg>
  );
}

/** A small client chip: a status dot plus the client name. */
function ClientChip({ label }: { readonly label: string }) {
  return (
    <span
      style={{
        ...MONO,
        display: "inline-flex",
        alignItems: "center",
        gap: 6,
        padding: "3px 9px",
        fontSize: 11,
        lineHeight: 1,
        color: nitro.text,
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: 999,
        whiteSpace: "nowrap",
      }}
    >
      <span
        aria-hidden="true"
        style={{
          width: 5,
          height: 5,
          borderRadius: 999,
          background: nitro.accentHover,
          flex: "none",
        }}
      />
      {label}
    </span>
  );
}

/**
 * Agentic-coding scene illustration (variant 3): the field-in-use callout.
 *
 * A self-contained GitHub-dark card showing the `Product.price` coordinate, an
 * "in use by 3 clients" usage line, and three client chips (Web, iOS, Admin):
 * the registry feedback an agent gets before changing a field. Fully static, the
 * settled final frame.
 */
export function FeedbackVariant3({ className }: FeedbackVariant3Props) {
  return (
    <div
      className={className}
      style={{
        width: "100%",
        maxWidth: 320,
        margin: "0 auto",
        background: nitro.bg,
        border: `1px solid ${nitro.border}`,
        borderRadius: nitro.radius,
        fontFamily: nitro.font,
        fontSize: 12,
        color: nitro.text,
        overflow: "hidden",
        userSelect: "none",
        boxShadow: "0 1px 3px rgba(2, 6, 16, 0.6)",
      }}
    >
      {/* Title row: the schema coordinate under inspection. */}
      <div
        style={{
          display: "flex",
          alignItems: "baseline",
          gap: 8,
          padding: "11px 14px",
          background: nitro.surface,
          borderBottom: `1px solid ${nitro.border}`,
        }}
      >
        <span style={{ ...MONO, fontSize: 12.5, color: nitro.textStrong }}>
          <span style={{ color: nitro.synType }}>Product</span>
          <span style={{ color: nitro.synPunct }}>.</span>
          <span style={{ color: nitro.synField }}>price</span>
        </span>
        <span
          style={{
            ...MONO,
            marginLeft: "auto",
            fontSize: 9.5,
            letterSpacing: "0.1em",
            textTransform: "uppercase",
            color: nitro.textSecondary,
          }}
        >
          field
        </span>
      </div>

      {/* Usage line: how many clients read this coordinate. */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "12px 14px 10px",
        }}
      >
        <UsersMark />
        <span style={{ fontSize: 12, color: nitro.text }}>
          In use by{" "}
          <span style={{ ...MONO, color: nitro.warning, fontWeight: 600 }}>
            {CLIENTS.length}
          </span>{" "}
          clients
        </span>
      </div>

      {/* Client chips: the specific consumers an agent would affect. */}
      <div
        style={{
          display: "flex",
          flexWrap: "wrap",
          gap: 7,
          padding: "0 14px 14px",
        }}
      >
        {CLIENTS.map((client) => (
          <ClientChip key={client.id} label={client.label} />
        ))}
      </div>
    </div>
  );
}
