import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { TEMPLATE_SUMMARIES } from "@/src/data/templates/templates";
import { TemplateStackArt } from "./TemplateStackArt";

const frame = { width: 400, aspectRatio: "16 / 9", borderRadius: 12, overflow: "hidden" } as const;

const meta = {
  title: "Components/Templates/TemplateStackArt",
  component: TemplateStackArt,
} satisfies Meta<typeof TemplateStackArt>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Federation: Story = {
  args: { products: ["hot-chocolate", "fusion"] },
  decorators: [
    (Story) => (
      <div style={frame}>
        <Story />
      </div>
    ),
  ],
};

export const AllTemplates: Story = {
  args: { products: ["hot-chocolate"] },
  render: () => (
    <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 400px)", gap: 16 }}>
      {TEMPLATE_SUMMARIES.map((template) => (
        <div key={template.slug} style={frame}>
          <TemplateStackArt products={template.products} />
        </div>
      ))}
    </div>
  ),
};
