import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Picture } from "@/src/design-system/Picture";
import { PhotoLightbox } from "./PhotoLightbox";

// Deterministic inline placeholders: no network (so the screenshot is stable in
// visual tests) and no text (so there is no cross-environment font rendering).
const swatch = (hue: number, width = 400, height = 600) =>
  "data:image/svg+xml," +
  encodeURIComponent(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}">` +
      `<rect width="100%" height="100%" fill="hsl(${hue},55%,45%)"/>` +
      `<circle cx="${width * 0.5}" cy="${height * 0.38}" r="${Math.min(width, height) * 0.28}" fill="hsl(${hue},65%,72%)"/>` +
      `</svg>`,
  );

const SAMPLE = Array.from({ length: 6 }, (_, i) => ({
  src: swatch(i * 60),
  alt: `Trip photo ${i + 1}`,
}));

const GRID_CLASS =
  "grid list-none grid-cols-2 gap-3 p-0 sm:grid-cols-3 lg:grid-cols-4";

// PhotoLightbox wraps arbitrary thumbnail markup; the only contract is that each
// clickable thumbnail is an `<a data-photo-index={i}>`. The popout image comes
// from the `images` prop, matched to the clicked thumbnail by that index.
const thumbnails = (images: readonly { src: string; alt: string }[]) =>
  images.map((image, index) => (
    <li key={`${image.alt}-${index}`} className="m-0 p-0">
      <a
        href={image.src}
        data-photo-index={index}
        className="group block cursor-zoom-in overflow-hidden rounded-lg"
      >
        <Picture
          src={image.src}
          alt={image.alt}
          className="aspect-[2/3] w-full object-cover transition-transform duration-300 group-hover:scale-105"
        />
      </a>
    </li>
  ));

const meta = {
  title: "Components/PhotoLightbox",
  component: PhotoLightbox,
  parameters: { layout: "padded" },
  decorators: [
    (Story) => (
      <div style={{ width: "min(100%, 1100px)" }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof PhotoLightbox>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    images: SAMPLE,
    className: GRID_CLASS,
    children: thumbnails(SAMPLE),
  },
};

// A single photo hides the previous/next controls and the counter.
export const SinglePhoto: Story = {
  args: {
    images: SAMPLE.slice(0, 1),
    className: GRID_CLASS,
    children: thumbnails(SAMPLE.slice(0, 1)),
  },
};
