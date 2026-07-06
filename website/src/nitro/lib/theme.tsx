/**
 * ThemeProvider — injects the token CSS once (idempotent) and scopes it under a root
 * `.nt-root[data-theme]` element. Every demo/story wraps its content in this so the
 * `--t-*` variables resolve and theme flips by swapping one attribute.
 */
import { useEffect } from "react";
import type { CSSProperties, ReactNode } from "react";
import { THEME_CSS, type ThemeName } from "./tokens";
import { AppMotionConfig } from "./motion";

const STYLE_ID = "nt-theme-vars";

function useThemeStyles() {
  useEffect(() => {
    if (typeof document === "undefined") return;
    if (document.getElementById(STYLE_ID)) return;
    const el = document.createElement("style");
    el.id = STYLE_ID;
    el.textContent = THEME_CSS;
    document.head.appendChild(el);
    // Intentionally not removed on unmount: shared, idempotent, cheap to keep.
  }, []);
}

export interface ThemeProviderProps {
  theme?: ThemeName;
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
  /** Force Motion's reduced-motion mode (toolbar / reduced-motion stories). */
  reducedMotion?: "user" | "always" | "never";
}

export function ThemeProvider({
  theme = "dark",
  children,
  className,
  style,
  reducedMotion = "user",
}: ThemeProviderProps) {
  useThemeStyles();
  return (
    <AppMotionConfig reducedMotion={reducedMotion}>
      <div
        className={`nt-root${className ? " " + className : ""}`}
        data-theme={theme}
        style={style}
      >
        {children}
      </div>
    </AppMotionConfig>
  );
}
