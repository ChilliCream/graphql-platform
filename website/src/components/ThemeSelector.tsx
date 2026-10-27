"use client";

import {
  useId,
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
}

const OPTIONS: readonly ThemeOption[] = [
  { value: "light", label: "Light", Icon: SunIcon },
  { value: "dark", label: "Dark", Icon: MoonIcon },
  { value: "auto", label: "Auto", Icon: SunMoonIcon },
];

const OPTION_SIZES = {
  compact: {
    group: "max-[1280px]:border-0 max-[1280px]:p-0",
    box: "h-8 w-8 max-[1280px]:h-7 max-[1280px]:w-7",
    icon: "h-4 w-4",
  },
  comfortable: { group: "", box: "h-11 w-11", icon: "h-5 w-5" },
} as const;

export function ThemeSelector({ size }: ThemeSelectorProps) {
  const { preference, setPreference } = useTheme();
  const name = useId();
  const sizes = OPTION_SIZES[size];

  return (
    <div
      role="radiogroup"
      aria-label="Appearance"
      className={`border-cc-card-border inline-flex flex-none items-center rounded-full border p-0.5 ${sizes.group}`}
    >
      {OPTIONS.map(({ value, label, Icon }) => (
        <label
          key={value}
          title={label}
          className={`text-cc-ink-dim hover:text-cc-heading has-checked:bg-cc-ink-faint has-checked:text-cc-heading has-focus-visible:outline-cc-accent relative inline-flex cursor-pointer items-center justify-center rounded-full transition-colors has-focus-visible:outline-2 has-focus-visible:outline-offset-1 ${sizes.box}`}
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
