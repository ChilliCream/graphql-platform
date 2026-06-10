import type { ReactNode } from "react";
import { Picture } from "@/src/design-system/Picture";

interface ContentSectionProps {
  title: ReactNode;
  text: ReactNode;
  image?: ReactNode;
  imageSrc?: string;
  imageAlt?: string;
  imageMaxWidth?: number;
  imagePosition?: "left" | "right" | "bottom";
}

function resolveImage(
  imageSrc?: string,
  imageAlt?: string,
  imageMaxWidth?: number,
  image?: ReactNode,
): ReactNode | undefined {
  if (image) return image;
  if (!imageSrc) return undefined;
  return (
    <Picture
      src={imageSrc}
      alt={imageAlt ?? ""}
      width={imageMaxWidth ?? 1200}
      height={Math.round((imageMaxWidth ?? 1200) * 0.6)}
      sizes="(max-width: 1024px) 100vw, 1024px"
      className="h-auto w-full max-w-full rounded-2xl"
      style={{ maxWidth: imageMaxWidth ? `${imageMaxWidth}px` : "1200px" }}
    />
  );
}

export function ContentSection({
  title,
  text,
  image: imageProp,
  imageSrc,
  imageAlt,
  imageMaxWidth,
  imagePosition = "bottom",
}: ContentSectionProps) {
  const image = resolveImage(imageSrc, imageAlt, imageMaxWidth, imageProp);
  if (!image) {
    return (
      <section className="py-16">
        <div className="mx-auto max-w-3xl text-center">
          <ContentHeader title={title} text={text} />
        </div>
      </section>
    );
  }

  if (imagePosition === "bottom") {
    return (
      <section className="py-16">
        <div className="mx-auto max-w-3xl text-center">
          <ContentHeader title={title} text={text} />
        </div>
        <div className="mt-10 flex justify-center">{image}</div>
      </section>
    );
  }

  return (
    <section className="py-16">
      <div
        className={`grid items-center gap-10 lg:grid-cols-2 ${
          imagePosition === "left" ? "lg:[&>*:first-child]:order-2" : ""
        }`}
      >
        <div>
          <ContentHeader title={title} text={text} />
        </div>
        <div>{image}</div>
      </div>
    </section>
  );
}

function ContentHeader({ title, text }: { title: ReactNode; text: ReactNode }) {
  return (
    <>
      <h2 className="text-3xl font-semibold tracking-tight text-cc-ink sm:text-4xl">
        {title}
      </h2>
      <div className="mt-4 text-base text-cc-ink-dim sm:text-lg">{text}</div>
    </>
  );
}
