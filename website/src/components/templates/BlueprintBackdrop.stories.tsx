import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { BlueprintBackdrop } from "./BlueprintBackdrop";

const meta = {
  title: "Components/Templates/BlueprintBackdrop",
  component: BlueprintBackdrop,
  decorators: [
    (Story) => (
      <div style={{ position: "relative", height: 900, overflow: "hidden", background: "#0b0f1a" }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof BlueprintBackdrop>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Sheet: Story = {
  args: { className: "text-cc-accent opacity-45" },
};
