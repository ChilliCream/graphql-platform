import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AppMotionConfig } from "@/src/nitro/lib/motion";

import { ControlPlaneConsole } from "./ControlPlaneConsole";

const meta = {
  title: "Nitro/ControlPlaneConsole",
  component: ControlPlaneConsole,
  parameters: { layout: "fullscreen" },
  argTypes: {
    className: { control: "text" },
  },
  args: {
    className: "mx-auto max-w-5xl",
  },
  decorators: [
    (Story) => (
      <AppMotionConfig reducedMotion="always">
        <div className="cc-content-dark p-10">
          <Story />
        </div>
      </AppMotionConfig>
    ),
  ],
} satisfies Meta<typeof ControlPlaneConsole>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
