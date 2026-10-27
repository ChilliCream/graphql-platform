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

declare global {
  interface Window {
    __ccTheme?: ThemeStore;
  }
}

// Keep this function self-contained: the head script runs the same implementation.
export function createThemeStore(
  browser: Window,
  document: Document,
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
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute(
        "content",
        state.resolvedTheme === "dark" ? "#0b0f1a" : "#f5f1ea",
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

export const themeScript = `window.__ccTheme=(${createThemeStore.toString()})(window,document);`;
