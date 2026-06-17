import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Closing call-to-action. The oversized display headline echoes the coffee
 * theme that runs through the page; the buttons give the otherwise open canvas
 * a clear next step.
 */
export function GrabADrink() {
  return (
    <section className="mx-auto max-w-4xl px-5 py-24 text-center sm:px-12 sm:py-32">
      <h2 className="hero text-cc-ink text-h2 sm:text-h1 leading-[1.05] font-bold text-balance">
        Want to grab a Drink?
      </h2>
      <p className="text-cc-prose mx-auto mt-6 max-w-2xl text-lg text-pretty sm:text-xl">
        Unify all your APIs into a comprehensive company graph, streamlining
        data accessibility and enhancing integration. Transform the way you
        manage and interact with your data.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Us
        </OutlineButton>
      </div>
    </section>
  );
}
