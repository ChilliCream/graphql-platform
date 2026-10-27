import { themeScript } from "./theme-preference";

export function ThemeScript() {
  return (
    <>
      <meta name="theme-color" content="#f5f1ea" suppressHydrationWarning />
      <script dangerouslySetInnerHTML={{ __html: themeScript }} />
    </>
  );
}
