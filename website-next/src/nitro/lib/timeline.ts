/**
 * Stage-based timeline. Author an animation as an ORDERED list of stages, each with its OWN
 * duration in milliseconds — the total is DERIVED by summing them. Nothing is expressed as a
 * fraction of a hardcoded global duration, so changing one stage's ms never rescales the others
 * and the total is always correct.
 *
 *   const TL = timeline([
 *     { name: 'establish', ms: 700 },
 *     { name: 'typeQuery', ms: 2200 },
 *     { name: 'moveToRun', ms: 1400 },
 *     { name: 'runClick',  ms: 250 },
 *     …
 *   ])
 *   export const MY_SCREEN_MS = TL.total   // feed this to SoloScreen / the reel tab
 *
 * Then derive the normalized progress (0..1) windows the motion layer needs:
 *   playWindow={TL.span('typeQuery')}              // [startP, endP]
 *   const RUN = TL.start('runClick')               // a single instant
 *   useTransform(progress, TL.span('moveToRun'), [xA, xB])   // a move over a stage
 *   TL.at('planExec', 0.5)                         // halfway through a stage
 *
 * The screen still consumes a normalized `progress` MotionValue (0..1); the timeline just turns
 * real per-stage times into those fractions. The DERIVED `total` is what makes the clock correct.
 */
export interface Stage {
  name: string;
  ms: number;
}

export interface Timeline {
  /** total duration in ms (sum of all stages) — feed to SoloScreen/reel */
  readonly total: number;
  /** normalized progress (0..1) at an absolute ms offset */
  p(ms: number): number;
  /** progress at the START of a stage */
  start(name: string): number;
  /** progress at the END of a stage */
  end(name: string): number;
  /** [startP, endP] normalized window of a stage */
  span(name: string): [number, number];
  /** progress at a fraction (0..1) through a stage (default 0 = its start) */
  at(name: string, frac?: number): number;
  /** stage duration in ms */
  ms(name: string): number;
}

export function timeline(stages: Stage[]): Timeline {
  const total = stages.reduce((s, x) => s + x.ms, 0) || 1;
  const bounds = new Map<string, { start: number; ms: number }>();
  let acc = 0;
  for (const s of stages) {
    bounds.set(s.name, { start: acc, ms: s.ms });
    acc += s.ms;
  }
  const get = (name: string) => {
    const b = bounds.get(name);
    if (!b) throw new Error(`timeline: unknown stage "${name}"`);
    return b;
  };
  const p = (ms: number) => ms / total;
  return {
    total,
    p,
    start: (name) => p(get(name).start),
    end: (name) => p(get(name).start + get(name).ms),
    span: (name) => [p(get(name).start), p(get(name).start + get(name).ms)],
    at: (name, frac = 0) => p(get(name).start + get(name).ms * frac),
    ms: (name) => get(name).ms,
  };
}
