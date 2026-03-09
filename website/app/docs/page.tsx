import { redirect } from "next/navigation";

import { getDocsConfig } from "@/lib/docs";

function getRedirectPath(): string {
  const products = getDocsConfig();
  const hotchocolate = products.find((p) => p.path === "hotchocolate");
  const target = hotchocolate || products[0];

  if (target) {
    const version = target.latestStableVersion;
    return `/docs/${target.path}${version ? `/${version}` : ""}`;
  }

  return "/";
}

export default function DocsIndex() {
  redirect(getRedirectPath());
}
