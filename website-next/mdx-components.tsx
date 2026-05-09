import type { MDXComponents } from "mdx/types";
import { Admonition } from "@/src/design-system/Admonition";
import { CodeBlock } from "@/src/design-system/CodeBlock";
import { CodeStep } from "@/src/design-system/CodeStep";
import { Divider } from "@/src/design-system/Divider";
import { InlineCode } from "@/src/design-system/InlineCode";
import { Link } from "@/src/design-system/Link";
import { List, ListItem } from "@/src/design-system/List";
import { Image } from "@/src/design-system/Image";
import { Quote } from "@/src/design-system/Quote";
import { detectAdmonition } from "@/src/helpers/detectAdmonition";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeaderCell,
  TableRow,
} from "@/src/design-system/Table";
import { Typography } from "@/src/design-system/Typography";

const components: MDXComponents = {
  h1: (props) => <Typography variant="h1" {...props} />,
  h2: (props) => <Typography variant="h2" {...props} />,
  h3: (props) => <Typography variant="h3" {...props} />,
  h4: (props) => <Typography variant="h4" {...props} />,
  h5: (props) => <Typography variant="h5" {...props} />,
  h6: (props) => <Typography variant="h6" {...props} />,

  p: (props) => <Typography variant="body" {...props} />,
  strong: (props) => <Typography variant="strong" {...props} />,
  em: (props) => <Typography variant="em" {...props} />,
  del: (props) => <Typography variant="del" {...props} />,

  a: Link,
  code: InlineCode,
  hr: Divider,

  ul: (props) => <List {...props} />,
  ol: (props) => <List ordered {...props} />,
  li: ListItem,

  blockquote: ({ children, ...props }) => {
    const alert = detectAdmonition(children);
    if (alert) {
      return <Admonition kind={alert.kind}>{alert.body}</Admonition>;
    }
    return <Quote {...props}>{children}</Quote>;
  },
  pre: CodeBlock,

  table: Table,
  thead: TableHead,
  tbody: TableBody,
  tr: TableRow,
  th: TableHeaderCell,
  td: TableCell,

  img: Image,

  CodeStep,
};

export function useMDXComponents(): MDXComponents {
  return components;
}
