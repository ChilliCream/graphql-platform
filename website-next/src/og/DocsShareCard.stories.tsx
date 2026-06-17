import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { PRODUCTS } from "@/src/data/products";
import { DocsShareCard } from "./DocsShareCard";

/**
 * Renders the 1200x630 card at half size so it fits the canvas while keeping
 * the fixed px typography that Satori produces in the real OG image.
 */
const meta = {
  title: "Share Cards/DocsShareCard",
  component: DocsShareCard,
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
} satisfies Meta<typeof DocsShareCard>;

export default meta;
type Story = StoryObj<typeof meta>;

/** Card args for a doc page belonging to a product, looked up by slug. */
function productArgs(slug: string, title: string) {
  const name = PRODUCTS.find((p) => p.slug === slug)?.title ?? slug;
  return { eyebrow: name, title, productSlug: slug };
}

// A typical doc page title.
export const Default: Story = {
  args: productArgs("fusion", "Introduction"),
};

// A long doc page title, to check wrapping within the frame.
export const LongTitle: Story = {
  args: productArgs(
    "fusion",
    "Composing a Distributed Schema with Fusion Gateways",
  ),
};

// A doc page title far too long for the frame, to show how overflow is handled.
export const OverflowingTitle: Story = {
  args: productArgs(
    "fusion",
    "Composing a Distributed Schema with Fusion Gateways, Source Schemas, Type Extensions, and Custom Resolver Pipelines",
  ),
};

export const Nitro: Story = {
  args: productArgs("nitro", "Get started"),
};

export const HotChocolate: Story = {
  args: productArgs("hotchocolate", "Get started"),
};

export const Fusion: Story = {
  args: productArgs("fusion", "Get started"),
};

export const StrawberryShake: Story = {
  args: productArgs("strawberryshake", "Get started"),
};

export const Mocha: Story = {
  args: productArgs("mocha", "Get started"),
};

export const Skillz: Story = {
  args: productArgs("skillz", "Get started"),
};
