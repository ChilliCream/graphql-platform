import { themeColors, themeScript } from "./theme-preference";

export function ThemeScript() {
  return (
    <>
      <meta
        name="theme-color"
        content={themeColors.light}
        media="(prefers-color-scheme: light)"
        suppressHydrationWarning
      />
      <meta
        name="theme-color"
        content={themeColors.dark}
        media="(prefers-color-scheme: dark)"
        suppressHydrationWarning
      />
      <script dangerouslySetInnerHTML={{ __html: themeScript }} />
    </>
  );
}
