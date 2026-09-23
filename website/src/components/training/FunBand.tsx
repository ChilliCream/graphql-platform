import { Band } from "@/src/components/Band";
import { CheckList } from "@/src/components/CheckList";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";

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
        <Card variant="plain" className="bg-cc-surface/60 p-6">
          <Eyebrow size="2xs" className="mb-3">
            What we will not do
          </Eyebrow>
          <CheckList columns={1} items={FUN_AVOID} />
        </Card>
      }
    />
  );
}
