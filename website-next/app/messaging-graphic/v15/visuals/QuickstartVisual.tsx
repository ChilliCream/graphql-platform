"use client";

import { useEffect, useLayoutEffect, useRef, useState } from "react";
import { CORAL, CORAL_SOFT, CYAN, NAVY, VIOLET } from "../palette";

// Spotlight loop: ~3.4s per panel; between steps a coral pulse slides down
// the rail from the previous via ring to the next.
const STEP = 3.4;
const PERIOD = STEP * 3;
const TRAVEL = 0.85;
const FADE_IN = 0.45;
const FADE_OUT = 0.4;
const FLASH_DUR = 0.75;
const TRAIL_OP = [0.3, 0.16, 0.07];
const TRAIL_R = [1.8, 1.4, 1];

const TAG_DIM = "rgba(139,160,188,0.6)";
const ACTIVE_BORDER = "rgba(240,120,106,0.4)";

const KW = { color: VIOLET };
const TY = { color: CYAN };
const MT = { color: CORAL };

interface Geom {
  readonly x: number;
  readonly ys: readonly [number, number, number];
}

function ease(t: number): number {
  const k = Math.min(Math.max(t, 0), 1);
  return k < 0.5 ? 4 * k * k * k : 1 - Math.pow(-2 * k + 2, 3) / 2;
}

const STEPS = [
  { num: "01", label: "A record is a message" },
  { num: "02", label: "A handler is an interface" },
  { num: "03", label: "Registered at compile time" },
] as const;

const PANEL_RECORD = (
  <>
    <div>
      <span style={KW}>public sealed record</span>
      <span style={TY}> OrderPlaced</span>
      <span className="text-cc-ink-dim">(</span>
    </div>
    <div>
      <span style={TY}>{"    Guid"}</span>
      <span className="text-cc-ink"> OrderId</span>
      <span className="text-cc-ink-dim">, </span>
      <span style={KW}>decimal</span>
      <span className="text-cc-ink"> Amount</span>
      <span className="text-cc-ink-dim">);</span>
    </div>
  </>
);

const PANEL_HANDLER = (
  <>
    <div>
      <span style={KW}>public class</span>
      <span style={TY}> OrderPlacedHandler</span>
      <span className="text-cc-ink-dim">(</span>
      <span style={TY}>AppDbContext</span>
      <span className="text-cc-ink"> db</span>
      <span className="text-cc-ink-dim">)</span>
    </div>
    <div>
      <span className="text-cc-ink-dim">{"    : "}</span>
      <span style={TY}>IEventHandler</span>
      <span className="text-cc-ink-dim">&lt;</span>
      <span style={TY}>OrderPlaced</span>
      <span className="text-cc-ink-dim">&gt;</span>
    </div>
    <div>
      <span className="text-cc-ink-dim">{"{"}</span>
    </div>
    <div>
      <span style={KW}>{"    public async"}</span>
      <span style={TY}> ValueTask</span>
      <span style={MT}> HandleAsync</span>
      <span className="text-cc-ink-dim">(</span>
    </div>
    <div>
      <span style={TY}>{"        OrderPlaced"}</span>
      <span className="text-cc-ink"> message</span>
      <span className="text-cc-ink-dim">, </span>
      <span style={TY}>CancellationToken</span>
      <span className="text-cc-ink"> ct</span>
      <span className="text-cc-ink-dim">{") { … }"}</span>
    </div>
    <div>
      <span className="text-cc-ink-dim">{"}"}</span>
    </div>
  </>
);

const PANEL_REGISTER = (
  <>
    <div>
      <span className="text-cc-ink">builder</span>
      <span className="text-cc-ink-dim">.</span>
      <span className="text-cc-ink">Services</span>
    </div>
    <div>
      <span className="text-cc-ink-dim">{"    ."}</span>
      <span style={MT}>AddMessageBus</span>
      <span className="text-cc-ink-dim">()</span>
    </div>
    <div>
      <span className="text-cc-ink-dim">{"    ."}</span>
      <span style={MT}>AddOrderService</span>
      <span className="text-cc-ink-dim">{"() // source-generated"}</span>
    </div>
    <div>
      <span className="text-cc-ink-dim">{"    ."}</span>
      <span style={MT}>AddRabbitMQ</span>
      <span className="text-cc-ink-dim">();</span>
    </div>
  </>
);

const PANELS = [PANEL_RECORD, PANEL_HANDLER, PANEL_REGISTER];

export function QuickstartVisual() {
  const rootRef = useRef<HTMLDivElement>(null);
  const railRef = useRef<HTMLDivElement>(null);
  const tagRefs = useRef<Array<HTMLDivElement | null>>([]);
  const panelRefs = useRef<Array<HTMLDivElement | null>>([]);
  const pulseRef = useRef<SVGGElement>(null);
  const trailRefs = useRef<Array<SVGCircleElement | null>>([]);
  const flashRef = useRef<SVGCircleElement>(null);
  const [geom, setGeom] = useState<Geom | null>(null);

  // The rail is measured off the live DOM (tag centers), so it stays correct
  // at any card width; the panels themselves scroll rather than reflow.
  useLayoutEffect(() => {
    const rail = railRef.current;
    const root = rootRef.current;
    if (!rail || !root) {
      return;
    }
    const measure = () => {
      const tags = tagRefs.current;
      if (tags.some((el) => !el)) {
        return;
      }
      const rr = rail.getBoundingClientRect();
      if (rr.height < 40) {
        return;
      }
      const x = Math.round(rr.width / 2);
      const ys = tags.map((el) => {
        const r = (el as HTMLDivElement).getBoundingClientRect();
        return Math.round(r.top + r.height / 2 - rr.top);
      }) as [number, number, number];
      setGeom((prev) =>
        prev &&
        prev.x === x &&
        prev.ys[0] === ys[0] &&
        prev.ys[1] === ys[1] &&
        prev.ys[2] === ys[2]
          ? prev
          : { x, ys },
      );
    };
    measure();
    const ro = new ResizeObserver(measure);
    ro.observe(root);
    for (const el of tagRefs.current) {
      if (el) {
        ro.observe(el);
      }
    }
    return () => ro.disconnect();
  }, []);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) {
      return;
    }

    const setSpot = (i: number, on: boolean) => {
      const tag = tagRefs.current[i];
      const panel = panelRefs.current[i];
      if (tag) {
        tag.style.color = on ? CORAL : TAG_DIM;
      }
      if (panel) {
        panel.style.borderColor = on ? ACTIVE_BORDER : "";
        panel.style.opacity = on ? "1" : "0.55";
      }
    };

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      // Static final frame: all three panels lit evenly, rail static.
      for (let i = 0; i < STEPS.length; i++) {
        setSpot(i, true);
      }
      return;
    }
    if (!geom) {
      return;
    }
    const g = geom;

    let raf = 0;
    let running = false;
    let inView = false;
    let phase = 0;
    let last = 0;
    let shown = -1;

    const step = (now: number) => {
      if (!running) {
        return;
      }
      const dt = Math.min((now - last) / 1000, 0.05);
      last = now;
      phase = (phase + dt) % PERIOD;

      const idx = Math.min(2, Math.floor(phase / STEP));
      if (idx !== shown) {
        shown = idx;
        for (let i = 0; i < STEPS.length; i++) {
          setSpot(i, i === idx);
        }
      }
      const u = phase - idx * STEP;

      // pulse: fades in at the top via, slides down between steps, fades
      // out at the bottom of the cycle
      let y: number;
      let op = 1;
      let moving = false;
      let fromY = g.ys[0];
      if (idx === 0) {
        y = g.ys[0];
        op = Math.min(1, u / FADE_IN);
      } else {
        fromY = g.ys[idx - 1];
        if (u < TRAVEL) {
          moving = true;
          y = fromY + (g.ys[idx] - fromY) * ease(u / TRAVEL);
        } else {
          y = g.ys[idx];
        }
      }
      if (idx === 2 && u > STEP - FADE_OUT) {
        op = Math.max(0, (STEP - u) / FADE_OUT);
      }
      const pulse = pulseRef.current;
      if (pulse) {
        pulse.setAttribute("transform", `translate(${g.x} ${y})`);
        pulse.style.opacity = String(op);
      }

      trailRefs.current.forEach((c, j) => {
        if (!c) {
          return;
        }
        const ty = y - 9 * (j + 1);
        if (!moving || ty < fromY) {
          c.style.opacity = "0";
          return;
        }
        c.setAttribute("cx", String(g.x));
        c.setAttribute("cy", String(ty));
        c.style.opacity = String(TRAIL_OP[j]);
      });

      // arrival ring on the via the pulse just reached
      const k = (u - (idx === 0 ? FADE_IN : TRAVEL)) / FLASH_DUR;
      const fl = flashRef.current;
      if (fl) {
        if (k >= 0 && k <= 1) {
          fl.setAttribute("cy", String(g.ys[idx]));
          fl.setAttribute("r", String(3 + 11 * k));
          fl.style.opacity = String(0.35 * (1 - k) * (1 - k));
        } else {
          fl.style.opacity = "0";
        }
      }

      raf = requestAnimationFrame(step);
    };

    const sync = () => {
      const should = inView && !document.hidden;
      if (should && !running) {
        running = true;
        last = performance.now();
        raf = requestAnimationFrame(step);
      } else if (!should && running) {
        running = false;
        cancelAnimationFrame(raf);
      }
    };
    const io = new IntersectionObserver(
      (entries) => {
        inView = entries[entries.length - 1]?.isIntersecting ?? false;
        sync();
      },
      { threshold: 0.15 },
    );
    io.observe(root);
    const onVis = () => sync();
    document.addEventListener("visibilitychange", onVis);
    return () => {
      running = false;
      cancelAnimationFrame(raf);
      io.disconnect();
      document.removeEventListener("visibilitychange", onVis);
    };
  }, [geom]);

  return (
    <div
      ref={rootRef}
      aria-hidden
      className="border-cc-card-border bg-cc-card-bg relative flex h-auto w-full flex-col overflow-hidden rounded-2xl border p-5 backdrop-blur sm:h-[500px]"
    >
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

      <div className="flex min-h-0 flex-1 gap-3">
        {/* progress rail: three via rings joined by one copper lane */}
        <div ref={railRef} className="relative w-4 shrink-0">
          <svg
            className="absolute inset-0 h-full w-full overflow-visible"
            role="presentation"
          >
            {geom && (
              <g>
                <line
                  x1={geom.x}
                  y1={geom.ys[0]}
                  x2={geom.x}
                  y2={geom.ys[2]}
                  stroke="rgba(139,160,188,0.4)"
                  strokeWidth={1.5}
                />
                {geom.ys.map((y, i) => (
                  <circle
                    key={STEPS[i].num}
                    cx={geom.x}
                    cy={y}
                    r={2.5}
                    fill={NAVY}
                    stroke="rgba(139,160,188,0.5)"
                    strokeWidth={1}
                  />
                ))}
                {TRAIL_R.map((r, j) => (
                  <circle
                    key={j}
                    ref={(el) => {
                      trailRefs.current[j] = el;
                    }}
                    r={r}
                    fill={CORAL}
                    style={{ opacity: 0 }}
                  />
                ))}
                <g ref={pulseRef} style={{ opacity: 0 }}>
                  <circle r={7} fill={CORAL} opacity={0.16} />
                  <circle r={2.5} fill={CORAL} />
                  <circle r={1} fill={CORAL_SOFT} />
                </g>
                <circle
                  ref={flashRef}
                  cx={geom.x}
                  r={0}
                  fill="none"
                  stroke={CORAL}
                  strokeWidth={1.5}
                  style={{ opacity: 0 }}
                />
              </g>
            )}
          </svg>
        </div>

        <div className="flex min-h-0 min-w-0 flex-1 flex-col justify-between gap-3">
          {STEPS.map((s, i) => (
            <div key={s.num} className="flex min-w-0 flex-col">
              <div
                ref={(el) => {
                  tagRefs.current[i] = el;
                }}
                className="mb-1.5 font-mono text-[0.7rem] tracking-[0.15em] uppercase"
                style={{
                  color: i === 0 ? CORAL : TAG_DIM,
                  transition: "color .3s",
                }}
              >
                <span className="opacity-60">{s.num}</span> {s.label}
              </div>
              <div
                ref={(el) => {
                  panelRefs.current[i] = el;
                }}
                className="border-cc-card-border/60 overflow-x-auto rounded-lg border bg-black/30 p-4 font-mono text-[12px] leading-[1.65]"
                style={{
                  scrollbarWidth: "none",
                  transition: "border-color .3s, opacity .35s",
                  borderColor: i === 0 ? ACTIVE_BORDER : undefined,
                  opacity: i === 0 ? undefined : 0.55,
                }}
              >
                <div className="whitespace-pre">{PANELS[i]}</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
