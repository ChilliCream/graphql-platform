import Link from "next/link";
import { FromOurBlog } from "@/src/components/FromOurBlog";

export default function Home() {
  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-12">
        <section>
          <h2>Docs</h2>
          <ul>
            <li>
              <Link href="/docs/example/getting-started">Example</Link>
            </li>
          </ul>
        </section>

        <FromOurBlog />
      </div>
    </div>
  );
}
