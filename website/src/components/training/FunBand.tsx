import { Band } from "@/src/components/Band";
import { CheckList } from "@/src/components/CheckList";
import { SectionHeading } from "@/src/components/SectionHeading";

const FUN_FACTS = [
  "Plenty of breaks, by design",
  "Pair and group exercises",
  "Real schemas, not lorem ipsum",
  "Questions welcome, including the basic ones",
  "Working sessions on your real codebase",
  "Optional recap doc after the week",
];

const FUN_AVOID = [
  "No slide marathons",
  "No certificate factory",
  "No copy-paste exercises",
  "No graded tests at the end of the week",
  "No vendor pitch dressed up as training",
];

/**
 * The honesty band: the warm pitch for how the sessions actually feel on the
 * left, and a short list of what we will not do on the right.
 */
export function FunBand() {
  return (
    <Band
      skin="warm"
      className="py-16 sm:py-20"
      main={
        <div>
          <SectionHeading
            title="And yes, have lots of fun."
            description={
              <>
                Training that nobody enjoys does not stick. We run sessions like
                the workshops we wish we had been to: hands on, slightly
                informal, no slide marathons, room for questions that start with
                &ldquo;this is probably stupid but...&rdquo; (it is not).
              </>
            }
          />
          <CheckList className="mt-6" columns={2} items={FUN_FACTS} />
        </div>
      }
      aside={
        <div className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-6">
          <div className="text-cc-nav-label mb-3 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            What we will not do
          </div>
          <CheckList columns={1} items={FUN_AVOID} />
        </div>
      }
    />
  );
}
