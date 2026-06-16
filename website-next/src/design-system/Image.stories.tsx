import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Image } from "./Image";

const meta = {
  title: "Design System/Image",
  component: Image,
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 640 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Image>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    src: "https://placehold.co/640x360/png?text=Demo+Image",
    alt: "Demo placeholder image",
    width: 640,
    height: 360,
  },
};

export const Tall: Story = {
  args: {
    src: "https://placehold.co/400x600/png?text=Portrait",
    alt: "Portrait placeholder image",
    width: 400,
    height: 600,
  },
};

/**
 * When the source fails to load, the image swaps in the same broken-link
 * placeholder used for unavailable videos, sized by the image's intrinsic
 * aspect ratio.
 */
export const Broken: Story = {
  args: {
    src: "https://example.invalid/missing.png",
    alt: "An image that fails to load",
    width: 640,
    height: 360,
  },
};

/**
 * Inside a card (e.g. a blog teaser), the placeholder adopts the image's own
 * layout classes and fills the card's media frame edge to edge.
 */
export const BrokenInCard: Story = {
  args: {
    src: "https://example.invalid/missing.png",
    alt: "An image that fails to load",
    width: 640,
    height: 360,
    className: "h-full w-full object-cover",
  },
  decorators: [
    (Story) => (
      <div className="border-cc-ink-faint overflow-hidden rounded-2xl border">
        <div className="border-cc-ink-faint aspect-video w-full overflow-hidden border-b">
          <Story />
        </div>
        <div className="text-cc-ink-dim px-7 py-6 text-sm">Card content</div>
      </div>
    ),
  ],
};

/**
 * In tiny frames like avatars, the icon shrinks and the message is kept for
 * screen readers only.
 */
export const BrokenAvatar: Story = {
  args: {
    src: "https://example.invalid/missing.png",
    alt: "An avatar that fails to load",
    width: 30,
    height: 30,
    className: "h-[30px] w-[30px] rounded-full object-cover",
  },
};
