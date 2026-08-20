import type { Decorator, Meta, StoryObj } from "@storybook/nextjs-vite";
import { TEMPLATE_SUMMARIES } from "@/src/data/templates/templates";
import { TemplateCard } from "./TemplateCard";

const meta = {
  title: "Components/Templates/TemplateCard",
  component: TemplateCard,
} satisfies Meta<typeof TemplateCard>;

export default meta;
type Story = StoryObj<typeof meta>;

const singleCard: Decorator = (Story) => (
  <div style={{ width: 400 }}>
    <Story />
  </div>
);

export const Federation: Story = {
  args: {
    template: {
      slug: "fusion-3-service-federation",
      title: "Fusion 3-Service Federation",
      tagline: "Three services, one graph.",
      topology: "federation",
      useCases: ["starter"],
      language: "dotnet",
      clients: ["none"],
      products: ["hot-chocolate", "fusion"],
      stack: ["postgres"],
      agentReady: false,
    },
  },
  decorators: [singleCard],
};

export const AgentReady: Story = {
  args: {
    template: {
      slug: "agent-ready-api",
      title: "Agent-Ready API",
      tagline: "A Hot Chocolate service that exposes itself as an MCP server.",
      topology: "solo",
      useCases: ["llm-mcp"],
      language: "dotnet",
      clients: ["none"],
      products: ["hot-chocolate", "nitro"],
      stack: ["mcp"],
      agentReady: true,
    },
  },
  decorators: [singleCard],
};

export const Catalog: Story = {
  args: Federation.args,
  render: () => (
    <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 400px)", gap: 24 }}>
      {TEMPLATE_SUMMARIES.map((template) => (
        <TemplateCard key={template.slug} template={template} />
      ))}
    </div>
  ),
};
