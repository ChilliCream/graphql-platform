import { FC, ReactElement, ReactNode, useLayoutEffect, useRef } from "react";

import { CookieConsent, Promo } from "@/components/misc";
import { GlobalStyle } from "@/style";
import { Main } from "./main";

export interface SiteLayoutProps {
  readonly children: ReactNode;
  readonly disableStars?: boolean;
}

export const SiteLayout: FC<SiteLayoutProps> = ({ children, disableStars }) => {
  return (
    <>
      <GlobalStyle />
      <Main>{children}</Main>
      <CookieConsent />
      <Promo />
      {!disableStars && <Stars />}
    </>
  );
};

function Stars(): ReactElement {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useLayoutEffect(() => {
    if (!canvasRef.current) {
      return;
    }

    const canvas = canvasRef.current;
    const ctx = canvas.getContext("2d");
    const numStars = 800;
    const speed = 0.25;
    let stars: Star[] = [];
    let pointerX = 0;
    let pointerY = 0;
    let pointerTargetX = 0;
    let pointerTargetY = 0;
    let scrollTiltY = 0;
    let scrollTargetY = 0;
    let scrollAccum = 0;
    let lastScrollTop = 0;
    let scrollIdleTimer: number | null = null;
    const pointerMax = 15;
    const scrollMax = 60;
    const scrollRampDistance = 400;

    function setCanvasSize() {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    }

    class Star {
      constructor(
        private x: number,
        private y: number,
        private z: number,
        private size: number
      ) {}

      update() {
        this.z -= speed;
        if (this.z <= 0) {
          this.reset();
        }
      }

      reset() {
        this.z = canvas.width;
        this.x = Math.random() * (canvas.width * 2) - canvas.width;
        this.y = Math.random() * (canvas.height * 2) - canvas.height;
        this.size = Math.random() * 2 + 1;
      }

      draw() {
        const x =
          (((this.x + pointerX) / this.z) * canvas.width) / 2 +
          canvas.width / 2;
        const y =
          (((this.y + pointerY + scrollTiltY) / this.z) * canvas.height) / 2 +
          canvas.height / 2;
        const radius = (1 - this.z / canvas.width) * this.size;

        if (ctx) {
          ctx.beginPath();
          ctx.arc(x, y, radius, 0, Math.PI * 2);
          ctx.fill();
        }
      }
    }

    function initStars() {
      stars = Array.from(
        { length: numStars },
        () =>
          new Star(
            Math.random() * (canvas.width * 2) - canvas.width,
            Math.random() * (canvas.height * 2) - canvas.height,
            Math.random() * canvas.width,
            Math.random() * 2 + 1
          )
      );
    }

    function updateStars() {
      stars.forEach((star) => star.update());
      pointerX += (pointerTargetX - pointerX) * 0.08;
      pointerY += (pointerTargetY - pointerY) * 0.08;
      const scrollEase = scrollTargetY === 0 ? 0.009 : 0.08;
      scrollTiltY += (scrollTargetY - scrollTiltY) * scrollEase;
    }

    function handlePointerMove(e: PointerEvent) {
      const nx = (e.clientX / window.innerWidth) * 2 - 1;
      const ny = (e.clientY / window.innerHeight) * 2 - 1;
      pointerTargetX = nx * pointerMax;
      pointerTargetY = ny * pointerMax;
    }

    function handleScroll(e: Event) {
      const target = e.target;
      const scrollTop =
        target instanceof HTMLElement ? target.scrollTop : window.scrollY;
      const delta = scrollTop - lastScrollTop;
      lastScrollTop = scrollTop;

      if (delta !== 0 && Math.sign(delta) !== Math.sign(scrollAccum)) {
        scrollAccum = 0;
      }
      scrollAccum = Math.max(
        -scrollRampDistance,
        Math.min(scrollRampDistance, scrollAccum + delta)
      );
      const t = Math.abs(scrollAccum) / scrollRampDistance;
      const eased = t * t * (3 - 2 * t);
      scrollTargetY = Math.sign(scrollAccum) * eased * scrollMax;

      if (scrollIdleTimer) {
        window.clearTimeout(scrollIdleTimer);
      }

      scrollIdleTimer = window.setTimeout(() => {
        scrollTargetY = 0;
        scrollAccum = 0;
      }, 150);
    }

    function drawStars() {
      if (ctx) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        ctx.fillStyle = "#f4ebcb";
        stars.forEach((star) => star.draw());
      }
    }

    function animate() {
      updateStars();
      drawStars();
      requestAnimationFrame(animate);
    }

    window.addEventListener("resize", () => {
      setCanvasSize();
      initStars();
    });
    window.addEventListener("pointermove", handlePointerMove, {
      passive: true,
    });
    window.addEventListener("scroll", handleScroll, {
      capture: true,
      passive: true,
    });

    setCanvasSize();
    initStars();
    animate();
  }, []);

  return (
    <canvas
      ref={canvasRef}
      style={{
        position: "absolute",
        top: 0,
        right: 0,
        bottom: 0,
        left: 0,
        zIndex: -2,
        width: "100vw",
        height: "100vh",
      }}
    />
  );
}
