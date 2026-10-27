import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { expect, userEvent, within } from "storybook/test";
import { ThemeProvider } from "@/src/theme/ThemeProvider";
import type { ThemePreference } from "@/src/theme/theme-preference";
import { ThemeSelector } from "./ThemeSelector";

const STORAGE_KEY = "cc-theme";

const meta = {
  title: "Components/ThemeSelector",
  component: ThemeSelector,
  parameters: { layout: "centered", preference: "auto" },
  args: { size: "compact" },
  decorators: [
    (Story) => (
      <ThemeProvider>
        <Story />
      </ThemeProvider>
    ),
  ],
  beforeEach: ({ parameters }) => {
    const root = document.documentElement;
    const previous = localStorage.getItem(STORAGE_KEY);
    localStorage.setItem(STORAGE_KEY, String(parameters.preference));
    window.__ccTheme = undefined;
    return () => {
      if (previous === null) {
        localStorage.removeItem(STORAGE_KEY);
      } else {
        localStorage.setItem(STORAGE_KEY, previous);
      }
      window.__ccTheme = undefined;
      root.dataset.theme = "dark";
      root.style.removeProperty("color-scheme");
    };
  },
} satisfies Meta<typeof ThemeSelector>;

export default meta;
type Story = StoryObj<typeof meta>;

function systemTheme() {
  return matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

function preferenceOf(value: ThemePreference) {
  return value === "auto" ? systemTheme() : value;
}

export const Auto: Story = {
  play: async ({ canvasElement }) => {
    const group = within(canvasElement).getByRole("radiogroup", {
      name: "Appearance",
    });
    expect(within(group).getByRole("radio", { name: "Auto" })).toBeChecked();
    expect(document.documentElement.dataset.theme).toBe(systemTheme());
  },
};

export const Light: Story = {
  parameters: { preference: "light" },
  play: async ({ canvasElement }) => {
    expect(
      within(canvasElement).getByRole("radio", { name: "Light" }),
    ).toBeChecked();
    expect(document.documentElement.dataset.theme).toBe("light");
  },
};

export const Dark: Story = {
  parameters: { preference: "dark" },
  play: async ({ canvasElement }) => {
    expect(
      within(canvasElement).getByRole("radio", { name: "Dark" }),
    ).toBeChecked();
    expect(document.documentElement.dataset.theme).toBe("dark");
  },
};

export const Comfortable: Story = {
  args: { size: "comfortable" },
};

export const KeyboardSelection: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const steps: readonly { name: string; value: ThemePreference }[] = [
      { name: "Light", value: "light" },
      { name: "Dark", value: "dark" },
      { name: "Auto", value: "auto" },
    ];

    await userEvent.tab();
    expect(canvas.getByRole("radio", { name: "Auto" })).toHaveFocus();

    for (const { name, value } of steps) {
      await userEvent.keyboard("{ArrowRight}");
      expect(canvas.getByRole("radio", { name })).toBeChecked();
      expect(canvas.getByRole("radio", { name })).toHaveFocus();
      expect(localStorage.getItem(STORAGE_KEY)).toBe(value);
      expect(document.documentElement.dataset.theme).toBe(preferenceOf(value));
    }
  },
};

export const InHeader: Story = {
  decorators: [
    (Story) => (
      <div className="border-cc-card-border bg-cc-card-bg flex h-18 w-[28rem] items-center justify-between gap-4 border-b px-4">
        <span className="text-cc-heading text-sm font-medium">Contact Us</span>
        <Story />
      </div>
    ),
  ],
};
