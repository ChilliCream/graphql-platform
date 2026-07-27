export interface Stage {
  name: string;
  ms: number;
}

export interface Timeline {
  readonly total: number;
  p(ms: number): number;
  start(name: string): number;
  end(name: string): number;
  span(name: string): [number, number];
  at(name: string, frac?: number): number;
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
