import { Meta, Story } from "@storybook/react";
import React from "react";
import { List } from "./list";
import { Panel } from "./panel";
import { Tab } from "./tab";
import { Tabs, TabsProps } from "./tabs";

export default {
  title: "Components/Tabs",
  component: Tabs,
} as Meta;

const Template: Story<TabsProps> = ({ children, defaultValue }) => (
  <Tabs defaultValue={defaultValue}>{children}</Tabs>
);

export const TwoTabs = Template.bind({});

TwoTabs.args = {
  defaultValue: "tab1",
  children: (
    <>
      <List>
        <Tab value="tab1">Tab One</Tab>
        <Tab value="tab2">Tab Two</Tab>
      </List>
      <Panel value="tab1">
        <p>
          Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam ligula
          dui, venenatis sagittis mi vel, dignissim vehicula nulla. Class aptent
          taciti sociosqu ad litora torquent per conubia nostra, per inceptos
          himenaeos. In hac habitasse platea dictumst. Proin tristique semper ex
          ac lobortis. Donec molestie convallis finibus. Integer laoreet
          dignissim semper.{" "}
        </p>
      </Panel>
      <Panel value="tab2">
        <p>Lorem ipsum</p>
      </Panel>
    </>
  ),
};
