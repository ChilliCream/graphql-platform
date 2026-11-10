"use client";

import {
  createContext,
  useContext,
  useSyncExternalStore,
  type ReactNode,
} from "react";
import {
  createThemeStore,
  type ThemePreference,
  type ThemeState,
  type ThemeStore,
} from "./theme-preference";

interface ThemeContextValue extends ThemeState {
  readonly setPreference: (preference: ThemePreference) => void;
}

interface ThemeProviderProps {
  readonly children: ReactNode;
}

const ThemeContext = createContext<ThemeContextValue | null>(null);
const serverState: ThemeState = { preference: "auto", resolvedTheme: "light" };

function getStore(): ThemeStore {
  return (window.__ccTheme ??= createThemeStore(window, document));
}

function subscribe(listener: () => void) {
  return getStore().subscribe(listener);
}

function getSnapshot() {
  return getStore().getSnapshot();
}

function getServerSnapshot() {
  return serverState;
}

function setPreference(preference: ThemePreference) {
  getStore().setPreference(preference);
}

export function ThemeProvider({ children }: ThemeProviderProps) {
  const state = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);

  return (
    <ThemeContext.Provider value={{ ...state, setPreference }}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const theme = useContext(ThemeContext);
  if (theme === null) {
    throw new Error("useTheme must be used within a ThemeProvider.");
  }
  return theme;
}
