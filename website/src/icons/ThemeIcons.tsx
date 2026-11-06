import type { ComponentPropsWithoutRef } from "react";

function ThemeGlyph(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    />
  );
}

const RAYS =
  "M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.3 5.3l1.4 1.4M17.3 17.3l1.4 1.4M5.3 18.7l1.4-1.4M17.3 6.7l1.4-1.4";

export function SunIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <ThemeGlyph {...props}>
      <circle cx="12" cy="12" r="4" />
      <path d={RAYS} />
    </ThemeGlyph>
  );
}

export function MoonIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <ThemeGlyph {...props}>
      <path d="M20.5 14.2A8.5 8.5 0 1 1 9.8 3.5a6.7 6.7 0 0 0 10.7 10.7z" />
    </ThemeGlyph>
  );
}

export function SunMoonIcon(props: ComponentPropsWithoutRef<"svg">) {
  return (
    <ThemeGlyph {...props}>
      <circle cx="8" cy="8" r="2.5" />
      <path d="M8 2v1M2 8h1M3.8 3.8l.7.7M12.2 3.8l-.7.7M3.8 12.2l.7-.7" />
      <path d="M22 17.2A5.4 5.4 0 1 1 16.2 11.3 4.2 4.2 0 0 0 22 17.2z" />
    </ThemeGlyph>
  );
}
