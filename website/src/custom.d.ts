declare module "*.svg" {
  const content: React.FC<React.SVGAttributes<SVGElement>>;
  export default content;
}

declare module "@/images/artwork/*.svg" {
  const content: Record<string, unknown>;
  export default content;
}

declare module "@/images/companies/*.svg" {
  const content: Record<string, unknown>;
  export default content;
}

declare module "@/images/logo/*.svg" {
  const content: Record<string, unknown>;
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
