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
      <circle cx="12" cy="12" r="4" />
      <path d="M12 8a4 4 0 0 1 0 8z" fill="currentColor" />
      <path d={RAYS} />
    </ThemeGlyph>
  );
}
