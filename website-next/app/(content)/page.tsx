import Link from "next/link";
import { FromOurBlog } from "@/src/components/FromOurBlog";

export default function Home() {
  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-12">
        <section>
          <Link href="/docs/hotchocolate/">Docs</Link>
        </section>

        <FromOurBlog />
      </div>
    </div>
  );
}
