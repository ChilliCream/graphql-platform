import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Picture } from "./Picture";

// Real asset from public/. In Storybook (NODE_ENV !== "production") the image
// manifest is not loaded, so getOptimizedImage returns null and Picture renders
// a plain <img> from this path rather than an optimized <picture> with AVIF/WebP
// <source> sets. The path is still served by Vite from public/, so it displays.
const SAMPLE_SRC = "/images/nitro/nitro-app.png";

const meta = {
  title: "Design System/Picture",
  component: Picture,
  parameters: { layout: "centered" },
  argTypes: {
    src: { control: "text" },
    alt: { control: "text" },
    className: { control: "text" },
    width: { control: "number" },
    height: { control: "number" },
    sizes: { control: "text" },
    priority: { control: "boolean" },
    title: { control: "text" },
    style: { control: "object" },
  },
  args: {
    src: SAMPLE_SRC,
    alt: "The Nitro app showing a GraphQL operation",
    width: 1200,
    height: 705,
    sizes: "(max-width: 1024px) 100vw, 1024px",
    priority: false,
  },
  decorators: [
    (Story) => (
      <div className="w-[600px] max-w-full">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Picture>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

// `priority` marks the image as the LCP candidate: eager loading, high fetch
// priority, and synchronous decoding (it also preloads the AVIF srcset when an
// optimized manifest is present, which it is not in Storybook).
export const Priority: Story = {
  args: {
    priority: true,
  },
};

// `className` styles the rendered <img> directly (the <picture> wrapper uses
// `display: contents`, so layout/aspect utilities apply to the image).
export const Rounded: Story = {
  args: {
    className: "h-auto w-full rounded-2xl border border-white/10 shadow-lg",
  },
};

// Inline `style` is forwarded to the <img>.
export const StyledInline: Story = {
  args: {
    style: { borderRadius: "9999px", objectFit: "cover", aspectRatio: "1 / 1" },
    className: "w-64",
  },
};

// With no `src` the component renders an empty <img> (alt only). Useful for
// verifying graceful handling of a missing source.
export const NoSource: Story = {
  args: {
    src: undefined,
    alt: "No image source provided",
  },
};
