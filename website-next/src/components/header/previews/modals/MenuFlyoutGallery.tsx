"use client";

import { FlyoutStructured } from "./FlyoutStructured";
import { InteractiveMenu } from "./InteractiveMenu";

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

export function MenuFlyoutGallery() {
  return (
    <div className="flex flex-col gap-10 py-2">
      <header>
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Menu / Preview
        </p>
        <h1 className="font-heading text-cc-heading text-h1 mt-4 font-semibold tracking-tight">
          A structured{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            main menu
          </span>
          .
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-lg leading-relaxed">
          Internal preview, not indexed. The real top menu, fully interactive:
          hover{" "}
          <span className="text-cc-heading">
            Platform, Services, Developers, or Company
          </span>{" "}
          and its dropdown opens as labeled columns with a featured card, drawn
          from the live section content. Pricing and Help stay plain links.
        </p>
      </header>

      <InteractiveMenu Panel={FlyoutStructured} />
    </div>
  );
}
