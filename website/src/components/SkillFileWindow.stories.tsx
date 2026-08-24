import type { Meta, StoryObj } from "@storybook/nextjs-vite";

import { SkillFileWindow } from "./SkillFileWindow";

const SKILLS = [
  {
    name: "graphql-schema-design",
    description:
      "Proposes SDL in design mode and audits schema diffs in review mode, following the team's conventions.",
  },
  {
    name: "prototype-feature",
    description: "Builds a clickable, local-only prototype with realistic mock data before any schema or backend work.",
  },
  {
    name: "prototype-to-contract",
    description: "Turns an accepted prototype into colocated GraphQL fragments and a contract for the backend.",
  },
] as const;

const meta = {
  title: "Components/SkillFileWindow",
  component: SkillFileWindow,
  parameters: { layout: "fullscreen" },
  argTypes: {
    name: { control: "text" },
    description: { control: "text" },
    className: { control: "text" },
  },
  args: {
    name: SKILLS[0].name,
    description: SKILLS[0].description,
    className: "max-w-sm",
  },
  decorators: [
    (Story) => (
      <div className="cc-content-dark p-10">
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof SkillFileWindow>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};

export const LongDescription: Story = {
  args: {
    name: "prototype-to-contract",
    description: SKILLS[2].description,
  },
};

export const SkillGrid: Story = {
  render: () => (
    <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
      {SKILLS.map((skill) => (
        <SkillFileWindow key={skill.name} name={skill.name} description={skill.description} className="flex-1" />
      ))}
    </div>
  ),
};
