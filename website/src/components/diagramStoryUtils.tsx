import type { Decorator } from "@storybook/nextjs-vite";
import { ThemeProvider } from "@/src/theme/ThemeProvider";
import {
  createThemeStore,
  type ThemePreference,
} from "@/src/theme/theme-preference";

// Diagrams read the resolved theme from the provider, so their stories drive
// the same store the site does and restore the preview's Dark default after.
export function storyThemePreference(preference: ThemePreference) {
  return () => {
    const store = (window.__ccTheme ??= createThemeStore(window, document));
    store.setPreference(preference);
    return () => store.setPreference("dark");
  };
}

export const withThemeProvider: Decorator = (Story) => (
  <ThemeProvider>
    <Story />
  </ThemeProvider>
);
