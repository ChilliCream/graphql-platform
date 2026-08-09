import { pageMetadata } from "@/src/helpers/pageMetadata";

import { ClientPage } from "./ClientPage";

export const metadata = pageMetadata({
  title: "Mocha: Messaging for .NET",
  description:
    "Mocha is a .NET messaging framework with a source-generated mediator for work inside a service and a message bus for work between services.",
  path: "/products/mocha",
});

export default function MochaPage() {
  return <ClientPage />;
}
