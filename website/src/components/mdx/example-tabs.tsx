import React, { FunctionComponent } from 'react';
import { Tabs } from './tabs';

export interface ExampleTabsComposition {
    Annotation: FunctionComponent;
    Code: FunctionComponent;
    Schema: FunctionComponent;
}

export const ExampleTabs: FunctionComponent & ExampleTabsComposition = ({
    children
}) => {
    return (
        <Tabs defaultValue={'annotation'}>
            <Tabs.List>
                <Tabs.Tab value="annotation">Annotation-based</Tabs.Tab>
                <Tabs.Tab value="code">Code-first</Tabs.Tab>
                <Tabs.Tab value="schema">Schema-first</Tabs.Tab>
            </Tabs.List>
            {children}
        </Tabs>
    )
};

const Annotation: FunctionComponent = (
    { children }
) => <Tabs.Panel value='annotation'>{children}</Tabs.Panel>

const Code: FunctionComponent = (
    { children }
) => <Tabs.Panel value='code'>{children}</Tabs.Panel>

const Schema: FunctionComponent = (
    { children }
) => <Tabs.Panel value='schema'>{children}</Tabs.Panel>

ExampleTabs.Annotation = Annotation;
ExampleTabs.Code = Code;
ExampleTabs.Schema = Schema;
