"use client";

import {
  useId,
  useSyncExternalStore,
  type ComponentType,
  type ComponentPropsWithoutRef,
} from "react";
import { MoonIcon, SunIcon, SunMoonIcon } from "@/src/icons/ThemeIcons";
import { useTheme } from "@/src/theme/ThemeProvider";
import type { ThemePreference } from "@/src/theme/theme-preference";

interface ThemeSelectorProps {
  readonly size: "compact" | "comfortable";
}

interface ThemeOption {
  readonly value: ThemePreference;
  readonly label: string;
  readonly Icon: ComponentType<ComponentPropsWithoutRef<"svg">>;
  readonly prehydrationHighlight: string;
}

// Until hydration the server-rendered radio is always Auto, so the highlight
// follows the preference the head bootstrap set on the root element instead.
const OPTIONS: readonly ThemeOption[] = [
  {
    value: "light",
    label: "Light",
    Icon: SunIcon,
    prehydrationHighlight:
      "[html[data-theme-preference=light]_&]:bg-cc-active-pill [html[data-theme-preference=light]_&]:text-cc-heading",
  },
  {
    value: "dark",
    label: "Dark",
    Icon: MoonIcon,
    prehydrationHighlight:
      "[html[data-theme-preference=dark]_&]:bg-cc-active-pill [html[data-theme-preference=dark]_&]:text-cc-heading",
  },
  {
    value: "auto",
    label: "Auto",
    Icon: SunMoonIcon,
    prehydrationHighlight:
      "[html:not([data-theme-preference=light]):not([data-theme-preference=dark])_&]:bg-cc-active-pill [html:not([data-theme-preference=light]):not([data-theme-preference=dark])_&]:text-cc-heading",
  },
];

const CHECKED_HIGHLIGHT =
  "has-checked:bg-cc-active-pill has-checked:text-cc-heading";

const OPTION_SIZES = {
  compact: {
    box: "h-8 w-8 max-[1280px]:h-7 max-[1280px]:w-7",
    icon: "h-4 w-4",
  },
  comfortable: { box: "h-11 w-11", icon: "h-5 w-5" },
} as const;

function subscribeToNothing() {
  return () => {};
}

function useHydrated() {
  return useSyncExternalStore(
    subscribeToNothing,
    () => true,
    () => false,
  );
}

export function ThemeSelector({ size }: ThemeSelectorProps) {
  const { preference, setPreference } = useTheme();
  const name = useId();
  const sizes = OPTION_SIZES[size];
  const hydrated = useHydrated();

  return (
    <div
      role="radiogroup"
      aria-label="Appearance"
      className="border-cc-card-border inline-flex flex-none items-center rounded-full border p-0.5"
    >
      {OPTIONS.map(({ value, label, Icon, prehydrationHighlight }) => (
        <label
          key={value}
          title={label}
          className={`text-cc-ink-dim hover:text-cc-heading ${hydrated ? CHECKED_HIGHLIGHT : prehydrationHighlight} has-focus-visible:outline-cc-accent relative inline-flex cursor-pointer items-center justify-center rounded-full transition-colors has-focus-visible:outline-2 has-focus-visible:outline-offset-1 ${sizes.box}`}
        >
          <input
            type="radio"
            name={name}
            value={value}
            checked={preference === value}
            onChange={() => setPreference(value)}
            className="absolute inset-0 m-0 cursor-pointer appearance-none opacity-0"
          />
          <Icon className={sizes.icon} aria-hidden="true" />
          <span className="sr-only">{label}</span>
        </label>
      ))}
    </div>
  );
}
