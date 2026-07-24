import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { CoffeeTray } from "./CoffeeTray";
import { CookieCrumble } from "./CookieCrumble";
import { Espresso } from "./Espresso";
import { Swirl } from "./Swirl";

const meta = {
  title: "Illustrations/Misc",
  decorators: (Story) => (
    <div style={{ maxWidth: "90%", width: 320 }}>
      <Story />
    </div>
  ),
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const CoffeeTrayStory: Story = {
  name: "CoffeeTray",
  render: () => <CoffeeTray />,
};

export const EspressoStory: Story = {
  name: "Espresso",
  render: () => <Espresso style={{ height: 320 }} />,
};

export const CookieCrumbleStory: Story = {
  name: "CookieCrumble",
  render: () => <CookieCrumble style={{ height: 320 }} />,
};

export const SwirlStory: Story = {
  name: "Swirl",
  render: () => <Swirl style={{ width: 120 }} />,
};
