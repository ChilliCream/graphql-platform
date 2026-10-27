import { themeScript } from "./theme-preference";

export function ThemeScript() {
  return (
    <>
      <meta name="theme-color" content="#f7f8fc" suppressHydrationWarning />
      <script dangerouslySetInnerHTML={{ __html: themeScript }} />
    </>
  );
}
