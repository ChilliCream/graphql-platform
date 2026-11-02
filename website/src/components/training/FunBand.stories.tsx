import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { FunBand } from "./FunBand";

const meta = {
  title: "Pages/Training/FunBand",
  component: FunBand,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-6xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof FunBand>;

export default meta;
type Story = StoryObj<typeof meta>;

// The honesty band: the warm version on the left, what we will not do on the right.
export const Default: Story = {};
