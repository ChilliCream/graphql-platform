import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { CompareTable } from "./CompareTable";

const meta = {
  title: "Pages/Pricing/CompareTable",
  component: CompareTable,
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-5xl">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof CompareTable>;

export default meta;
type Story = StoryObj<typeof meta>;

// Every tier as a column, capabilities grouped into labelled sections. The
// table scrolls horizontally below its min width.
export const Default: Story = {};
