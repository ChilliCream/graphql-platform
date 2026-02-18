import React from "react";

import { getRecentNitroBlogPostTeasers } from "@/lib/blog";
import NitroPage from "@/page-components/products/nitro";

export default function Page() {
  const recentPosts = getRecentNitroBlogPostTeasers();
  return <NitroPage recentPosts={recentPosts} />;
}
