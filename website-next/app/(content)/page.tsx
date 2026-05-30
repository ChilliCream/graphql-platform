import Link from "next/link";
import { FromOurBlog } from "@/src/components/FromOurBlog";
import { Typography } from "@/src/design-system/Typography";

export default function Home() {
  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto flex max-w-6xl flex-col gap-12">
        <section>
          <Typography variant="body">TODO: Landing page</Typography>
        </section>

        <FromOurBlog />
      </div>
    </div>
  );
}
