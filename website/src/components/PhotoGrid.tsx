import { Picture } from "@/src/design-system/Picture";
import { PhotoLightbox } from "@/src/components/PhotoLightbox";

interface PhotoGridImage {
  readonly src: string;
  readonly alt: string;
}

interface PhotoGridProps {
  readonly images: readonly PhotoGridImage[];
}

export function PhotoGrid({ images }: PhotoGridProps) {
  if (images.length === 0) {
    return null;
  }

  return (
    <PhotoLightbox
      images={images}
      className="my-8 grid list-none grid-cols-2 gap-3 p-0 sm:grid-cols-3 lg:grid-cols-4"
    >
      {images.map((image, index) => (
        <li key={`${image.src}-${index}`} className="m-0 p-0">
          <a
            href={image.src}
            data-photo-index={index}
            className="group block cursor-zoom-in overflow-hidden rounded-lg"
          >
            <Picture
              src={image.src}
              alt={image.alt}
              sizes="(max-width: 639px) 50vw, (max-width: 1023px) 33vw, 256px"
              className="aspect-[2/3] w-full object-cover transition-transform duration-300 group-hover:scale-105"
            />
          </a>
        </li>
      ))}
    </PhotoLightbox>
  );
}
