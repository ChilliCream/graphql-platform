import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { DripBrewer } from "./DripBrewer";
import { FrenchPress } from "./FrenchPress";
import { PourOver } from "./PourOver";

const meta = {
  title: "Illustrations/Coffee technology",
  decorators: (Story) => (
    <div style={{ maxWidth: "90%", width: 320 }}>
      <Story />
    </div>
  ),
} satisfies Meta;

export default meta;
type Story = StoryObj<typeof meta>;

export const FrenchPressStory: Story = {
  name: "FrenchPress",
  render: () => <FrenchPress />,
};

export const DripBrewerStory: Story = {
  name: "DripBrewer",
  render: () => <DripBrewer />,
};

export const PourOverStory: Story = {
  name: "PourOver",
  render: () => <PourOver />,
};
