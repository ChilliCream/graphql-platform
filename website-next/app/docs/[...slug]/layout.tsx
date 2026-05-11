import type { ReactNode } from "react";
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
    <div className="grid grid-cols-1 lg:grid-cols-[20rem_1fr]">
      <SidebarDrawer>
        <Sidebar tree={tree} currentPath={currentPath} />
      </SidebarDrawer>
      {children}
    </div>
  );
}
