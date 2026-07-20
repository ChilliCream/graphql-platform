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
  }, []);
}

export interface ThemeProviderProps {
  theme?: ThemeName;
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
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
