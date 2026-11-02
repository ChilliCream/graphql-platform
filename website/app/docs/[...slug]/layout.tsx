import type { ReactNode } from "react";
import { DocsToolbar } from "@/src/components/DocsToolbar";
import { Sidebar } from "@/src/components/Sidebar";
import { SidebarDrawer } from "@/src/components/SidebarDrawer";
import { buildContentTree } from "@/src/helpers/buildContentTree";
import { NOT_FOUND_SEGMENT } from "@/src/helpers/docsParams";

export default async function DocsLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;

  // The synthetic 404 pages render the standalone not-found body, so skip the
  // docs chrome (sidebar + toolbar) and let them sit in the root layout only.
  if (slug[slug.length - 1] === NOT_FOUND_SEGMENT) {
    return <>{children}</>;
  }

  const product = slug[0];
  const tree = buildContentTree(`docs/${product}`, `/docs/${product}`);
  const currentPath = `/docs/${slug.join("/")}`;

  return (
    <div
      data-docs-layout
      className="cc-content-dark grid h-full grid-cols-1 lg:grid-cols-[20rem_1fr]"
    >
      <SidebarDrawer>
        <Sidebar
          tree={tree}
          currentPath={currentPath}
          activeProduct={product}
        />
      </SidebarDrawer>
      <div className="min-w-0">
        <DocsToolbar />
        {children}
      </div>
    </div>
  );
}
