import { FromOurBlog } from "@/src/components/FromOurBlog";
import { HomeHero } from "@/src/components/home/HomeHero";

export default function Home() {
  return (
    <>
      <HomeHero />
      <div className="px-5 py-8 sm:px-12">
        <div className="mx-auto flex max-w-6xl flex-col gap-12">
          <FromOurBlog />
        </div>
      </div>
    </>
  );
}
