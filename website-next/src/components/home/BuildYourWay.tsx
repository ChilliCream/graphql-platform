import { CoffeeTray } from "@/src/icons/CoffeeTray";

/** Brand spectrum (cyan -> violet -> coral), reused from the hero accent. */
const CARD_GRADIENT =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 36%,#b681a9 64%,#f0786a 100%)";

/**
 * Full-bleed gradient feature card introducing the "stack your way" idea: the
 * coffee-tray illustration on one side, the headline and supporting copy on the
 * other. The card sits on the brand spectrum, so the text switches to the dark
 * page ink for contrast. Stacks to a single column on small screens.
 */
export function BuildYourWay() {
  return (
    <section className="mx-auto max-w-6xl px-5 py-8 sm:px-12">
      <div
        className="relative overflow-hidden rounded-3xl px-8 py-12 text-[#0b1018] sm:px-12 sm:py-14 lg:px-16"
        style={{ backgroundImage: CARD_GRADIENT }}
      >
        <div className="flex flex-col items-center gap-10 lg:flex-row lg:gap-16">
          <CoffeeTray className="h-44 w-auto flex-none drop-shadow-[0_18px_30px_rgba(0,0,0,0.25)] sm:h-52 lg:h-60" />
          <div className="max-w-xl text-center lg:text-left">
            <h2 className="font-heading text-h4 sm:text-h3 leading-[1.1] font-semibold tracking-[-0.01em]">
              Build your way.
              <br />
              Ship faster.
              <br />
              Stay in control.
            </h2>
            <p className="mt-5 text-base/relaxed text-[#0b1018]/75 sm:text-lg/relaxed">
              Pick what you need for your stack. Whether you&rsquo;re building a
              monolithic or federated GraphQL API, a message-intensive service,
              or an application, we&rsquo;ve got the right tools for you.
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}
