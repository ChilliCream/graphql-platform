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

declare module "gatsby-plugin-disqus" {
  interface DisqusProps {
    config: any;
  }

  export class Disqus extends React.Component<DisqusProps> {}

  interface CommentCountProps {
    config: any;
    placeholder: any;
  }

  export class CommentCount extends React.Component<CommentCountProps> {}
}
