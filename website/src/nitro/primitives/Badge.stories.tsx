import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Badge } from "./Badge";
import { ThemeProvider } from "../lib/theme";
import { token } from "../lib/tokens";
import { IconCheck, IconMutation, IconQuery } from "./icons";

const meta = {
  title: "Nitro/Primitives/Badge",
  component: Badge,
  parameters: { layout: "centered" },
  argTypes: {
    children: { control: "text" },
    icon: { control: false },
    style: { control: false },
  },
  args: {
    children: "142 ms",
  },
  decorators: [
    (Story) => (
      <ThemeProvider
        theme="dark"
        reducedMotion="always"
        className="w-[560px] max-w-full p-6"
      >
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof Badge>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Mono: Story = {
  args: {
    children: "142 ms",
    mono: true,
    background: token.bg,
    border: token.border,
    color: token.textSecondary,
  },
};

export const WithIcon: Story = {
  args: {
    children: "getUser",
    mono: true,
    icon: <IconMutation size={11} />,
    style: { gap: 5, padding: "2px 8px" },
  },
};

export const Danger: Story = {
  args: {
    children: "INTERNAL_SERVER_ERROR",
    mono: true,
    background: "rgba(207,34,46,0.14)",
    border: "#cf222e",
    color: token.errorText,
    style: { gap: 5, padding: "2px 8px" },
  },
};

export const SolidDark: Story = {
  args: {
    children: "client",
    lowercase: true,
    background: token.blue,
    border: false,
    color: token.surface,
  },
};

export const SolidLight: Story = {
  args: {
    children: "deployed",
    icon: <IconCheck size={11} color="#fff" />,
    bold: true,
    background: token.success,
    border: false,
    color: "#fff",
  },
};

export const TagWithIcon: Story = {
  args: {
    children: "v2.4.1",
    mono: true,
    icon: (
      <svg
        width={10}
        height={10}
        viewBox="0 0 16 16"
        fill="none"
        stroke={token.textSecondary}
        strokeWidth={1.4}
      >
        <path d="M2.5 2.5h5l6 6-5 5-6-6z" />
        <circle cx="5" cy="5" r="1" fill={token.textSecondary} stroke="none" />
      </svg>
    ),
    background: token.surface,
    border: token.border,
    color: token.text,
  },
};

export const Approved: Story = {
  args: {
    children: "Approved",
    icon: <IconCheck size={10} color={token.successText} />,
    bold: true,
    size: "xs",
    background: "transparent",
    border: token.success,
    color: token.successText,
  },
};

export const Outline: Story = {
  args: {
    children: "batched · 2",
    size: "xs",
    background: "transparent",
    border: token.border,
    color: token.textSecondary,
  },
};

export const Solid: Story = {
  args: {
    children: "Batch: 3",
    size: "xs",
    background: "rgba(88,166,255,0.18)",
    border: false,
    color: token.blue,
  },
};

export const SquareIcon: Story = {
  args: {
    square: true,
    icon: <IconQuery size={11} />,
    border: token.icQuery,
    color: token.icQuery,
  },
};

export const SquareLetter: Story = {
  args: {
    square: true,
    letter: "Q",
    mono: true,
    size: "xs",
    background: token.surface,
    border: token.border,
    color: token.textSecondary,
  },
};
