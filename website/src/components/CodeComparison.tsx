import { Children, isValidElement, type ReactNode } from "react";
import { CodeComparisonPane } from "@/src/components/CodeComparisonPane";
import { parseCodeBlockMeta } from "@/src/helpers/parseCodeBlockMeta";

interface CodeComparisonProps {
  /** Two fenced code blocks, each labelled with `filename="..."`. */
  readonly children: ReactNode;
}

/**
 * Two code blocks next to each other when the content column is wide enough
 * and stacked otherwise, for showing the same task written with two libraries.
 *
 * The first block shows our product and gets the accent. A later block whose
 * label names the same tool gets it too, so "Mocha: ordering service" next to
 * "Mocha: billing service" does not read as Mocha against a rival.
 */
export function CodeComparison({ children }: CodeComparisonProps) {
  const blocks = Children.toArray(children).filter(isValidElement);
  const ours = toolName(blocks[0]);

  return (
    <div className="@container my-8">
      <div className="grid gap-4 @min-[62rem]:grid-cols-2">
        {blocks.map((block, index) => (
          <CodeComparisonPane
            key={block.key}
            accent={index === 0 || (!!ours && toolName(block) === ours)}
          >
            {block}
          </CodeComparisonPane>
        ))}
      </div>
    </div>
  );
}

/**
 * The tool a code block's `filename` label names: the text before any colon,
 * e.g. "Mocha" for "Mocha: billing service".
 */
function toolName(block: ReactNode): string | undefined {
  if (!isValidElement<{ children?: ReactNode }>(block)) {
    return undefined;
  }
  const code = block.props.children;
  if (!isValidElement<{ "data-meta"?: string }>(code)) {
    return undefined;
  }
  const { filename } = parseCodeBlockMeta(code.props["data-meta"] ?? "");
  return filename?.split(":")[0].trim();
}
