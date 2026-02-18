declare module "@mdx-js/react" {
  import { Component, ComponentType, ReactNode } from "react";

  export interface MDXProviderProps {
    children: ReactNode;
    components: Record<any, ComponentType<any>>;
  }

  export class MDXProvider extends Component<MDXProviderProps> {}
}

declare module "*.svg" {
  const content: React.FC<React.SVGAttributes<SVGElement>>;
  export default content;
}

type Sprite = Record<"id" | "viewBox", string>;

declare module "@/images/artwork/*.svg" {
  const content: Sprite;
  export default content;
}

declare module "@/images/companies/*.svg" {
  const content: Sprite;
  export default content;
}

declare module "@/images/icons/*.svg" {
  const content: Sprite;
  export default content;
}

declare module "@/images/logo/*.svg" {
  const content: Sprite;
  export default content;
}
