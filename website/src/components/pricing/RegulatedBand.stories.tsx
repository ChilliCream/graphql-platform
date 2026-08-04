import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { RegulatedBand } from "./RegulatedBand";

const meta = {
  title: "Pages/Pricing/RegulatedBand",
  component: RegulatedBand,
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
} satisfies Meta<typeof RegulatedBand>;

export default meta;
type Story = StoryObj<typeof meta>;

// The pitch band for regulated / air-gapped teams: copy and CTAs on the left,
// a checklist of what we handle on the right.
export const Default: Story = {};
