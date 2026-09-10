import type { ReactNode } from "react";

type ChromeHeader =
  | { readonly variant: "dots" }
  | { readonly variant: "status-dot"; readonly color?: string }
  | { readonly variant: "custom"; readonly content: ReactNode };

type ChromeShadow = "2xl" | "soft" | "none";

interface ChromeGlow {
  /** CSS background value for the blurred halo, e.g. a radial-gradient. */
  readonly background: string;
  /** Tailwind inset utilities for the halo's footprint, e.g. "-inset-3". */
  readonly inset?: string;
  /** Tailwind blur utility, e.g. "blur-2xl" (default) or "blur-3xl". */
  readonly blur?: string;
  /** Tailwind rounding utility for the halo shape. Defaults to "rounded-[2rem]". */
  readonly rounded?: string;
}

interface MockWindowChromeProps {
  /** Left side of the header bar. Omit (along with `label` and `headerRight`) for no header. */
  readonly header?: ChromeHeader;
  /** Mono label shown after the header's left content (e.g. dots or a status dot). */
  readonly label?: ReactNode;
  /** Right-aligned header content, e.g. a trailing status word or a badge. */
  readonly headerRight?: ReactNode;
  /**
   * Replaces the default header layout classes (`flex items-center gap-2
   * px-4 py-2.5`) for sites whose bar has its own background, padding, or
   * `justify-between` layout. The `border-cc-card-border border-b` are
   * always applied underneath.
   */
  readonly headerClassName?: string;
  /** Optional footer bar, rendered below `children` with the same chrome styling as the header. */
  readonly footer?: ReactNode;
  /** Replaces the default footer layout classes, same rules as `headerClassName`. */
  readonly footerClassName?: string;
  /** Decorative blurred halo behind the window. Omit for no glow. */
  readonly glow?: ChromeGlow;
  /** Drop shadow under the window: "2xl" (shadow-2xl), "soft" (wide diffuse), or "none". */
  readonly shadow?: ChromeShadow;
  /** Corner rounding of the window itself. Defaults to "rounded-2xl". */
  readonly rounded?: string;
  /** Extra classes on the window surface (background, e.g. override `bg-cc-surface` with `bg-cc-card-bg`). */
  readonly surfaceClassName?: string;
  /** Extra classes on the outermost wrapper (the glow's positioning context). */
  readonly className?: string;
  readonly children?: ReactNode;
}

const SHADOW_CLASS: Record<ChromeShadow, string> = {
  "2xl": "shadow-2xl shadow-black/50",
  soft: "shadow-[0_28px_70px_-28px_rgba(0,0,0,0.7)]",
  none: "",
};

const DOT_COLORS = ["#ff5f57", "#febc2e", "#28c840"];

function ChromeHeaderLeft({ header }: { readonly header: ChromeHeader }) {
  if (header.variant === "dots") {
    return (
      <span aria-hidden="true" className="flex items-center gap-1.5">
        {DOT_COLORS.map((color) => (
          <span
            key={color}
            className="inline-block h-2 w-2 rounded-full"
            style={{ background: color }}
          />
        ))}
      </span>
    );
  }
  if (header.variant === "status-dot") {
    return (
      <span
        aria-hidden="true"
        className="inline-block h-2 w-2 rounded-full"
        style={{ background: header.color ?? "var(--color-cc-accent)" }}
      />
    );
  }
  return <>{header.content}</>;
}

/**
 * The mock browser/app window shell used for product visuals across the
 * marketing site: an optional glow halo, a bordered surface, an optional
 * header bar (traffic-light dots, a single status dot, or fully custom
 * content), and an optional matching footer bar.
 *
 * The header and footer bars default to the border-only, `px-4 py-2.5` shape
 * shared by most sites. Pass `headerClassName` / `footerClassName` when a
 * site's bar needs its own background, padding, or `justify-between` layout.
 */
export function MockWindowChrome({
  header,
  label,
  headerRight,
  headerClassName,
  footer,
  footerClassName,
  glow,
  shadow = "2xl",
  rounded = "rounded-2xl",
  surfaceClassName,
  className,
  children,
}: MockWindowChromeProps) {
  const hasHeader =
    header !== undefined || label !== undefined || headerRight !== undefined;

  return (
    <div className={["relative", className].filter(Boolean).join(" ")}>
      {glow && (
        <div
          aria-hidden="true"
          className={[
            "pointer-events-none absolute -z-10 opacity-40",
            glow.inset ?? "-inset-6",
            glow.blur ?? "blur-2xl",
            glow.rounded ?? "rounded-[2rem]",
          ].join(" ")}
          style={{ background: glow.background }}
        />
      )}
      <div
        className={[
          "border-cc-card-border overflow-hidden border",
          surfaceClassName ?? "bg-cc-surface",
          rounded,
          SHADOW_CLASS[shadow],
        ]
          .filter(Boolean)
          .join(" ")}
      >
        {hasHeader && (
          <div
            className={[
              "border-cc-card-border border-b",
              headerClassName ?? "flex items-center gap-2 px-4 py-2.5",
            ]
              .filter(Boolean)
              .join(" ")}
          >
            {header && <ChromeHeaderLeft header={header} />}
            {label !== undefined && (
              <span className="text-cc-ink-dim font-mono text-[10px] tracking-[0.18em] uppercase">
                {label}
              </span>
            )}
            {headerRight !== undefined && (
              <span className="ml-auto flex shrink-0 items-center">
                {headerRight}
              </span>
            )}
          </div>
        )}
        {children}
        {footer && (
          <div
            className={[
              "border-cc-card-border border-t",
              footerClassName ?? "flex items-center gap-2 px-4 py-2.5",
            ]
              .filter(Boolean)
              .join(" ")}
          >
            {footer}
          </div>
        )}
      </div>
    </div>
  );
}
