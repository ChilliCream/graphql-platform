declare module "@mdx-js/react" {
  import { Component, ComponentType, ReactNode } from "react";

  export interface MDXProviderProps {
    children: ReactNode;
    components: Record<any, ComponentType<any>>;
  }

  export class MDXProvider extends Component<MDXProviderProps> {}
}
