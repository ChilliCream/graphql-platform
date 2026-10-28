export type ThemePreference = "auto" | "light" | "dark";
export type ResolvedTheme = "light" | "dark";

export interface ThemeState {
  readonly preference: ThemePreference;
  readonly resolvedTheme: ResolvedTheme;
}

export interface ThemeStore {
  readonly getSnapshot: () => ThemeState;
  readonly subscribe: (listener: () => void) => () => void;
  readonly setPreference: (preference: ThemePreference) => void;
}

export interface ThemeColors {
  readonly light: string;
  readonly dark: string;
}

export const themeColors: ThemeColors = {
  light: "#f5f1ea",
  dark: "#0b0f1a",
};

declare global {
  interface Window {
    __ccTheme?: ThemeStore;
  }
}

export function createThemeStore(
  browser: Window,
  document: Document,
): ThemeStore {
  return createThemeRuntime(browser, document, themeColors);
}

// Keep this function self-contained: the head script runs the same implementation.
function createThemeRuntime(
  browser: Window,
  document: Document,
  colors: ThemeColors,
): ThemeStore {
  const storageKey = "cc-theme";
  const systemTheme = browser.matchMedia("(prefers-color-scheme: dark)");
  const listeners = new Set<() => void>();
  let preference: ThemePreference = "auto";

  function normalize(value: unknown): ThemePreference {
    return value === "light" || value === "dark" ? value : "auto";
  }

  function resolve(value: ThemePreference): ResolvedTheme {
    return value === "auto" ? (systemTheme.matches ? "dark" : "light") : value;
  }

  try {
    preference = normalize(browser.localStorage.getItem(storageKey));
  } catch {
    // Unavailable storage uses Auto until the user chooses an in-memory override.
  }

  let state: ThemeState = { preference, resolvedTheme: resolve(preference) };

  function apply() {
    document.documentElement.dataset.theme = state.resolvedTheme;
    document.documentElement.style.colorScheme = state.resolvedTheme;
    document
      .querySelectorAll('meta[name="theme-color"]')
      .forEach((meta) =>
        meta.setAttribute("content", colors[state.resolvedTheme]),
      );
  }

  function update(value: ThemePreference) {
    const resolvedTheme = resolve(value);
    if (state.preference === value && state.resolvedTheme === resolvedTheme) {
      return;
    }
    state = { preference: value, resolvedTheme };
    apply();
    listeners.forEach((listener) => listener());
  }

  apply();
  systemTheme.addEventListener("change", () => update(state.preference));

  return {
    getSnapshot: () => state,
    subscribe(listener) {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },
    setPreference(value) {
      const preference = normalize(value);
      try {
        browser.localStorage.setItem(storageKey, preference);
      } catch {
        // The current choice remains active when persistence is unavailable.
      }
      update(preference);
    },
  };
}

export const themeScript = `window.__ccTheme=(${createThemeRuntime.toString()})(window,document,${JSON.stringify(themeColors)});`;
