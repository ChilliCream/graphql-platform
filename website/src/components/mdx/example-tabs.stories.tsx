import { Meta, Story } from "@storybook/react";
import React from "react";
import { Code, ExampleTabs, Implementation, Schema } from "./example-tabs";

export default {
  title: "Components/ExampleTabs",
  component: ExampleTabs,
} as Meta;

const Template: Story = () => (
  <ExampleTabs>
    <Implementation>
      <p>Implementation</p>
    </Implementation>
    <Code>
      <p>Code</p>
    </Code>
    <Schema>
      <p>Schema</p>
    </Schema>
  </ExampleTabs>
);

export const Basic = Template.bind({});
