import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { StrokeIcon } from "./StrokeIcon";
import { ThemeProvider } from "../../lib/theme";
import { token } from "../../lib/tokens";

const FOLDER_PATH =
  "M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z";

const meta = {
  title: "Nitro/Primitives/StrokeIcon",
  component: StrokeIcon,
  parameters: { layout: "centered" },
  args: {
    d: FOLDER_PATH,
  },
  decorators: [
    (Story) => (
      <ThemeProvider theme="dark" reducedMotion="always" className="p-6">
        <Story />
      </ThemeProvider>
    ),
  ],
} satisfies Meta<typeof StrokeIcon>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const Small: Story = {
  args: {
    size: 12,
    strokeWidth: 1.5,
  },
};

export const Large: Story = {
  args: {
    size: 24,
    strokeWidth: 1.7,
  },
};

export const FilledOverride: Story = {
  args: {
    fill: token.surface,
  },
};

export const ColorOverride: Story = {
  args: {
    color: "#d9a521",
  },
};
