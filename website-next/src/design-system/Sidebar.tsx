import Link from "next/link";
import type { TreeNode } from "@/src/helpers/buildContentTree";

export function Sidebar({ tree }: { tree: TreeNode[] }) {
  return (
    <nav className="flex flex-col gap-1 px-5 py-6 text-sm">
      <Tree nodes={tree} depth={0} />
    </nav>
  );
}

function Tree({ nodes, depth }: { nodes: TreeNode[]; depth: number }) {
  return (
    <ul className="flex flex-col gap-1">
      {nodes.map((node) => (
        <li key={`${node.href ?? node.title}-${depth}`}>
          {node.href ? (
            <Link
              href={node.href}
              className="block rounded px-3 py-1.5 text-stone-700 hover:bg-stone-100 hover:text-stone-900 transition-colors"
              style={{ paddingLeft: `${depth * 0.75 + 0.75}rem` }}
            >
              {node.title}
            </Link>
          ) : (
            <span
              className="block px-3 py-1.5 font-semibold text-stone-900"
              style={{ paddingLeft: `${depth * 0.75 + 0.75}rem` }}
            >
              {node.title}
            </span>
          )}
          {node.children.length > 0 ? (
            <Tree nodes={node.children} depth={depth + 1} />
          ) : null}
        </li>
      ))}
    </ul>
  );
}
