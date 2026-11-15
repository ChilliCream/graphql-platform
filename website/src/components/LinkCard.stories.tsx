import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { Fusion, FUSION_ARTWORK } from "@/src/icons/Fusion";

import { LinkCard } from "./LinkCard";
import { ProductArtworkIcon } from "./ProductArtworkIcon";

const meta = {
  title: "Components/LinkCard",
  component: LinkCard,
  parameters: { layout: "fullscreen" },
  argTypes: {
    variant: {
      control: "select",
      options: ["trailing", "plain", "icon"],
    },
    href: { control: "text" },
    title: { control: "text" },
    description: { control: "text" },
    external: { control: "boolean" },
    icon: { control: false },
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <div className="mx-auto max-w-sm">
          <Story />
        </div>
      </div>
    ),
  ],
} satisfies Meta<typeof LinkCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Trailing: Story = {
  args: {
    variant: "trailing",
    href: "/platform/analytics",
    title: "Analytics",
    description: "Instant Insights. Enhanced Performance.",
  },
};

export const Plain: Story = {
  args: {
    variant: "plain",
    href: "/legal/privacy-policy",
    title: "Privacy Policy",
    description: "How we handle your data.",
  },
};

export const PlainExternal: Story = {
  args: {
    variant: "plain",
    href: "https://store.chillicream.com",
    title: "Shop",
    description: "ChilliCream merch and goodies.",
    external: true,
  },
};

/** Matches the docs index cards: the sheet scale their icon tiles use. */
const PRODUCT_ICON_SHEET_REM = 2;

export const Icon: Story = {
  args: {
    variant: "icon",
    href: "/docs/fusion",
    title: "Fusion",
    description: "Compose services into one unified GraphQL API.",
    icon: (
      <ProductArtworkIcon
        Icon={Fusion}
        artwork={FUSION_ARTWORK}
        slotHeightRem={PRODUCT_ICON_SHEET_REM}
      />
    ),
  },
  render: (args) => (
    <ul>
      <LinkCard {...args} />
    </ul>
  ),
};
