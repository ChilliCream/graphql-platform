import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { AGENTS, AgentLogo } from "./AgentLogo";

const meta = {
  title: "Components/AgentLogo",
  component: AgentLogo,
  parameters: { layout: "fullscreen" },
  argTypes: {
    agent: { control: "object" },
    className: { control: "text" },
  },
  args: {
    agent: AGENTS[0],
    className: "size-8",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof AgentLogo>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const AllAgents: Story = {
  render: () => (
    <ul className="grid max-w-md grid-cols-2 gap-x-6 gap-y-3.5 sm:grid-cols-3">
      {AGENTS.map((agent) => (
        <li key={agent.slug} className="flex items-center gap-2.5">
          <AgentLogo agent={agent} className="size-6 shrink-0" />
          <span className="text-cc-ink font-mono text-sm">{agent.name}</span>
        </li>
      ))}
    </ul>
  ),
};
