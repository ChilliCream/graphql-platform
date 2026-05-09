"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import type { TreeNode } from "@/src/helpers/buildContentTree";

export function Sidebar({ tree }: { tree: TreeNode[] }) {
  const pathname = usePathname();
  return (
    <nav className="flex flex-col gap-1 px-5 py-6 text-sm">
      <ul className="flex flex-col gap-1">
        {tree.map((node, i) => (
          <NodeView
            key={`${node.href ?? node.title}-${i}`}
            node={node}
            depth={0}
            currentPath={pathname}
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
  const [expanded, setExpanded] = useState(containsCurrent);
  const isActive = node.href === currentPath;
  const padLeft = `${depth * 0.75 + 0.75}rem`;

  return (
    <li>
      <div className="flex items-stretch">
        {node.href ? (
          <Link
            href={node.href}
            aria-current={isActive ? "page" : undefined}
            className={`block flex-1 rounded px-3 py-1.5 transition-colors ${
              isActive
                ? "bg-stone-100 text-stone-900"
                : "text-stone-700 hover:bg-stone-100 hover:text-stone-900"
            }`}
            style={{ paddingLeft: padLeft }}
          >
            {node.title}
          </Link>
        ) : (
          <button
            type="button"
            onClick={() => setExpanded((v) => !v)}
            className="block flex-1 rounded px-3 py-1.5 text-left text-stone-700 transition-colors hover:bg-stone-100 hover:text-stone-900"
            style={{ paddingLeft: padLeft }}
          >
            {node.title}
          </button>
        )}
        {hasChildren ? (
          <button
            type="button"
            aria-label={expanded ? "Collapse section" : "Expand section"}
            aria-expanded={expanded}
            onClick={() => setExpanded((v) => !v)}
            className="inline-flex w-7 items-center justify-center rounded text-stone-500 hover:bg-stone-100 hover:text-stone-900"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              className={`h-4 w-4 transition-transform ${expanded ? "rotate-90" : ""}`}
              aria-hidden="true"
            >
              <path
                fillRule="evenodd"
                d="M7.21 14.77a.75.75 0 0 1 .02-1.06L10.94 10 7.23 6.29a.75.75 0 1 1 1.06-1.06l4.24 4.24a.75.75 0 0 1 0 1.06l-4.24 4.24a.75.75 0 0 1-1.08-.02Z"
                clipRule="evenodd"
              />
            </svg>
          </button>
        ) : null}
      </div>
      {hasChildren && expanded ? (
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
      ) : null}
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
