import type { ReactNode } from "react";
import { DocsToolbar } from "@/src/design-system/DocsToolbar";
import { Sidebar } from "@/src/design-system/Sidebar";
import { SidebarDrawer } from "@/src/design-system/SidebarDrawer";
import { buildContentTree } from "@/src/helpers/buildContentTree";

export default async function DocsLayout({
  children,
  params,
}: {
  children: ReactNode;
  params: Promise<{ slug: string[] }>;
}) {
  const { slug } = await params;
  const product = slug[0];
  const tree = buildContentTree(`docs/${product}`, `/docs/${product}`);
  const currentPath = `/docs/${slug.join("/")}`;

  return (
    <div className="cc-content-dark grid min-h-[calc(100vh-72px)] grid-cols-1 lg:grid-cols-[20rem_1fr]">
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
