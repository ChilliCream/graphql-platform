import Link from "next/link";
import type { TreeNode } from "@/src/helpers/buildContentTree";

export function Sidebar({
  tree,
  currentPath,
}: {
  tree: TreeNode[];
  currentPath: string;
}) {
  return (
    <nav className="flex flex-col gap-1 px-5 py-6 text-sm">
      <ul className="flex flex-col gap-1">
        {tree.map((node, i) => (
          <NodeView
            key={`${node.href ?? node.title}-${i}`}
            node={node}
            depth={0}
            currentPath={currentPath}
          />
        ))}
      </ul>
    </nav>
  );
}

function NodeView({
  node,
  depth,
  currentPath,
}: {
  node: TreeNode;
  depth: number;
  currentPath: string;
}) {
  const hasChildren = node.children.length > 0;
  const containsCurrent = subtreeContains(node, currentPath);
  const childMatchesCurrent = node.children.some(
    (c) => c.href === currentPath
  );
  const isActive = node.href === currentPath && !childMatchesCurrent;
  const padLeft = `${depth * 0.75 + 0.75}rem`;

  const label = node.href ? (
    <Link
      href={node.href}
      aria-current={isActive ? "page" : undefined}
      className={`block flex-1 rounded px-3 py-1.5 transition-colors ${
        isActive
          ? "bg-emerald-50 font-medium text-emerald-700"
          : "text-slate-700 hover:bg-slate-100 hover:text-slate-900"
      }`}
      style={{ paddingLeft: padLeft }}
    >
      {node.title}
    </Link>
  ) : (
    <span
      className="block flex-1 cursor-pointer rounded px-3 py-1.5 text-left text-slate-700 transition-colors hover:bg-slate-100 hover:text-slate-900"
      style={{ paddingLeft: padLeft }}
    >
      {node.title}
    </span>
  );

  if (!hasChildren) {
    return <li>{label}</li>;
  }

  return (
    <li>
      <details open={containsCurrent} className="group">
        <summary className="flex list-none items-stretch [&::-webkit-details-marker]:hidden">
          {label}
          <span
            aria-hidden="true"
            className="inline-flex w-7 cursor-pointer items-center justify-center rounded text-slate-500 hover:bg-slate-100 hover:text-slate-900"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              className="h-4 w-4 transition-transform group-open:rotate-90"
            >
              <path
                fillRule="evenodd"
                d="M7.21 14.77a.75.75 0 0 1 .02-1.06L10.94 10 7.23 6.29a.75.75 0 1 1 1.06-1.06l4.24 4.24a.75.75 0 0 1 0 1.06l-4.24 4.24a.75.75 0 0 1-1.08-.02Z"
                clipRule="evenodd"
              />
            </svg>
          </span>
        </summary>
        <ul className="flex flex-col gap-1">
          {node.children.map((child, i) => (
            <NodeView
              key={`${child.href ?? child.title}-${i}`}
              node={child}
              depth={depth + 1}
              currentPath={currentPath}
            />
          ))}
        </ul>
      </details>
    </li>
  );
}

function subtreeContains(node: TreeNode, currentPath: string): boolean {
  if (node.href === currentPath) {
    return true;
  }
  if (node.href && currentPath.startsWith(`${node.href}/`)) {
    return true;
  }
  for (const child of node.children) {
    if (subtreeContains(child, currentPath)) {
      return true;
    }
  }
  return false;
}
