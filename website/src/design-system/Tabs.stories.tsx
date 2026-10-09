import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { Tab, Tabs } from "./Tabs";

const meta = {
  title: "Design System/Tabs",
  component: Tabs,
  argTypes: {
    defaultIndex: {
      control: { type: "number", min: 0 },
    },
  },
} satisfies Meta<typeof Tabs>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    defaultIndex: 1,
    children: [
      <Tab key="first" label="First">
        First tab content.
      </Tab>,
      <Tab key="second" label="Second">
        Second tab content (selected by default).
      </Tab>,
      <Tab key="third" label="Third">
        Third tab content.
      </Tab>,
    ],
  },
};
