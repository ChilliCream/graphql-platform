import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { EnterpriseBand } from "./EnterpriseBand";

const meta = {
  title: "Pages/Support/EnterpriseBand",
  component: EnterpriseBand,
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
} satisfies Meta<typeof EnterpriseBand>;

export default meta;
type Story = StoryObj<typeof meta>;

// Accent-bordered enterprise panel with the tailored-terms checklist.
export const Default: Story = {};
