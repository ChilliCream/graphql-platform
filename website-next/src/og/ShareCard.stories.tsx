import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { ShareCard } from "./ShareCard";

/**
 * Renders the 1200x630 card at half size so it fits the canvas while keeping
 * the fixed px typography that Satori produces in the real OG image.
 */
const meta = {
  title: "Share Cards/ShareCard",
  component: ShareCard,
  parameters: {
    layout: "centered",
  },
  decorators: [
    (Story) => (
      <div style={{ background: "#ffffff", padding: 32 }}>
        <div style={{ width: 600, height: 315, overflow: "hidden" }}>
          <div
            style={{
              width: 1200,
              height: 630,
              transform: "scale(0.5)",
              transformOrigin: "top left",
            }}
          >
            <Story />
          </div>
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof ShareCard>;

export default meta;
type Story = StoryObj<typeof meta>;

// Homepage card: the bare hero artwork, no page title.
export const WithoutTitle: Story = {
  args: {},
};

// Marketing page card: a divider and the page title under the headline.
export const WithTitle: Story = {
  args: {
    pageTitle: "Pricing",
  },
};

// A long page title, to check it stays on one line within the frame.
export const LongTitle: Story = {
  args: {
    pageTitle: "Continuous Integration and Deployment",
  },
};

// A title far too long for the frame, to show how overflow is handled.
export const OverflowingTitle: Story = {
  args: {
    pageTitle:
      "Continuous Integration and Continuous Deployment Pipelines for Federated GraphQL Gateways at Enterprise Scale",
  },
};
