import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { PhotoGrid } from "./PhotoGrid";

const photo = (seed: string, width = 400, height = 600) => ({
  src: `https://picsum.photos/seed/${seed}/${width}/${height}`,
  alt: `Trip photo ${seed}`,
});

const SAMPLE = [
  photo("sf1"),
  photo("sf2"),
  photo("sf3"),
  photo("sf4"),
  photo("sf5"),
  photo("sf6"),
  photo("sf7"),
  photo("sf8"),
];

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
      photo("wide-1", 900, 450),
      photo("tall-1", 400, 720),
      photo("square-1", 500, 500),
      photo("wide-2", 960, 420),
      photo("tall-2", 380, 660),
      photo("square-2", 480, 480),
    ],
  },
};
