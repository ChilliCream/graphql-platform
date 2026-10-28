import { lightDark } from "@/src/components/lightDark";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

interface LineageMilestone {
  readonly when: string;
  readonly name: string;
  readonly detail?: string;
}

const DELAY_CLASSES = ["delay-0", "delay-150", "delay-300"];

const HUE = lightDark("#06b6d4", "var(--color-cc-success)");

const success = (light: number, dark: number) =>
  `color-mix(in oklab, ${HUE} ${lightDark(`${light}%`, `${dark}%`)}, transparent)`;

const LAST_HALO = lightDark(
  "0 0 4px rgb(6 182 212 / 0.55), 0 0 14px #06b6d4",
  "0 0 14px var(--color-cc-success)",
);

const MILESTONES: readonly LineageMilestone[] = [
  { when: "2019", name: "Apollo Federation" },
  { when: "2023", name: "GraphQL Fusion spec", detail: "ChilliCream · open" },
  {
    when: "now",
    name: "GraphQL Federation spec",
    detail: "GraphQL Foundation · open",
  },
];

/**
 * Timeline for the From Apollo section: Apollo Federation, GraphQL Fusion and
 * the GraphQL Federation spec on one vertical connector.
 */
export function SpecLineageTimeline() {
  return (
    <ol className="w-fit max-w-full min-w-0">
      {MILESTONES.map(({ when, name, detail }, i) => {
        const last = i === MILESTONES.length - 1;
        return (
          <li key={name} className="flex gap-4 sm:gap-5">
            <div className="flex shrink-0 flex-col items-center">
              <span
                className="flex h-8 w-8 items-center justify-center rounded-full border sm:h-9 sm:w-9"
                style={
                  last
                    ? {
                        borderColor: HUE,
                        backgroundColor: success(20, 20),
                        boxShadow: LAST_HALO,
                      }
                    : {
                        borderColor: success(80, 40),
                        backgroundColor: success(10, 10),
                      }
                }
              >
                <RevealOnScroll
                  className={`flex ${DELAY_CLASSES[i]}`}
                  hiddenClassName="scale-50 opacity-0 motion-reduce:scale-100"
                  shownClassName="scale-100 opacity-100"
                >
                  <span
                    aria-hidden="true"
                    className={`rounded-full ${last ? "h-3 w-3" : "h-2 w-2"}`}
                    style={{ background: last ? HUE : success(90, 50) }}
                  />
                </RevealOnScroll>
              </span>
              {!last && (
                <span
                  aria-hidden="true"
                  className="my-1.5 min-h-6 w-px flex-1"
                  style={{ background: success(60, 25) }}
                />
              )}
            </div>
            <div className={`min-w-0 ${last ? "" : "pb-6 sm:pb-7"}`}>
              <p className="text-cc-ink-dim font-mono text-sm">{when}</p>
              <p className="font-heading text-cc-heading text-h6 font-semibold text-balance">
                {name}
              </p>
              {detail !== undefined && (
                <p className="text-cc-ink-dim mt-1 font-mono text-sm">
                  {detail}
                </p>
              )}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
