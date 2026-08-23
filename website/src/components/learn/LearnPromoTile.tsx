import Link from "next/link";
import { Picture } from "@/src/design-system/Picture";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

interface LearnPromoTileImageProps {
  readonly variant: "image";
  readonly href: string;
  readonly image: string;
  readonly kicker: string;
  readonly title: string;
  readonly author?: string;
}

interface LearnPromoTileCtaProps {
  readonly variant: "cta";
  readonly href: string;
  readonly kicker: string;
  readonly title: string;
  readonly description: string;
}

type LearnPromoTileProps = LearnPromoTileImageProps | LearnPromoTileCtaProps;

/**
 * Curated right-rail unit (learn-editorial.md section 14.3), two variants:
 * a dark image tile with a bottom scrim, and a solid-accent CTA banner. The
 * CTA banner is the only solid-accent surface in the learn system, reserved
 * for one curated action per page. Content is passed as props; the component
 * never picks its own content.
 */
export function LearnPromoTile(props: LearnPromoTileProps) {
  if (props.variant === "cta") {
    return (
      <Link
        href={props.href}
        className="group/promo bg-cc-accent text-cc-surface flex min-h-36 flex-col gap-4 rounded-2xl p-6 no-underline"
      >
        <span className="font-mono text-xs tracking-wider uppercase opacity-70">{props.kicker}</span>
        <p className="text-sm opacity-90">{props.description}</p>
        <span className="mt-auto flex items-center justify-between gap-3">
          <span className="font-heading font-semibold">{props.title}</span>
          <ArrowRightIcon className="size-5 shrink-0 transition-transform group-hover/promo:translate-x-1" />
        </span>
      </Link>
    );
  }

  return (
    <Link
      href={props.href}
      className="group/promo border-cc-ink-faint relative block aspect-[4/3] overflow-hidden rounded-2xl border no-underline"
    >
      <Picture
        src={props.image}
        alt=""
        sizes="(max-width: 1279px) 50vw, 19rem"
        className="h-full w-full object-cover"
      />
      <span
        aria-hidden="true"
        className="from-cc-surface via-cc-surface/95 absolute inset-x-0 bottom-0 h-3/4 bg-gradient-to-t to-transparent"
      />
      <span className="absolute inset-x-0 bottom-0 flex flex-col gap-1 p-6">
        <span className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">{props.kicker}</span>
        <span className="font-heading text-cc-heading line-clamp-2 font-semibold">{props.title}</span>
        {props.author ? <span className="text-cc-ink-dim truncate text-sm">{props.author}</span> : null}
      </span>
    </Link>
  );
}
