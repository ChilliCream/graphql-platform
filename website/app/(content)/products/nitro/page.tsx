import { pageMetadata } from "@/src/helpers/pageMetadata";

import { ClientPage } from "./ClientPage";

export const metadata = pageMetadata({
  title: "Nitro",
  description:
    "Nitro is the API operations platform for teams that need observability, tracing, schema governance, client safety, and release checks in one control plane.",
  path: "/products/nitro",
});

export default function NitroPage() {
  return <ClientPage />;
}
