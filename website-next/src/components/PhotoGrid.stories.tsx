import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { PhotoGrid } from "./PhotoGrid";

// Deterministic inline placeholders: no network (so the screenshot is stable in
// visual tests) and no text (so there is no cross-environment font rendering).
// The grid forces a uniform 2:3 cell, so the source dimensions only affect how
// each placeholder is cropped.
const swatch = (hue: number, width = 400, height = 600) =>
  "data:image/svg+xml," +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">` +
      `<rect width="100%" height="100%" fill="hsl(${hue},55%,45%)"/>` +
      `<circle cx="${width * 0.5}" cy="${height * 0.38}" r="${Math.min(width, height) * 0.28}" fill="hsl(${hue},65%,72%)"/>` +
      `</svg>`,
  );

const photo = (index: number, width?: number, height?: number) => ({
  src: swatch(index * 45, width, height),
  alt: `Trip photo ${index + 1}`,
});

const SAMPLE = Array.from({ length: 8 }, (_, i) => photo(i));

const meta = {
  title: "Components/PhotoGrid",
  component: PhotoGrid,
  parameters: { layout: "padded" },
  decorators: [
    (Story) => (
      <div style={{ width: "min(100%, 1100px)" }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PhotoGrid>;

export default meta;
type Story = StoryObj<typeof meta>;

// Click any thumbnail to open the popout (Esc / arrow keys / buttons navigate).
export const Default: Story = {
  args: { images: SAMPLE },
};

export const ThreePhotos: Story = {
  args: { images: SAMPLE.slice(0, 3) },
};

// Sources with different aspect ratios (landscape, portrait, square) still line
// up on a uniform 2:3 grid because every cell uses object-cover.
export const MixedAspectRatios: Story = {
  args: {
    images: [
      photo(0, 900, 450),
      photo(1, 400, 720),
      photo(2, 500, 500),
      photo(3, 960, 420),
      photo(4, 380, 660),
      photo(5, 480, 480),
    ],
  },
};
