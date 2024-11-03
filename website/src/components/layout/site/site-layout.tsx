import { MDXProvider } from "@mdx-js/react";
import React, {
  FC,
  ReactElement,
  ReactNode,
  useLayoutEffect,
  useRef,
} from "react";

import { BlockQuote } from "@/components/mdx/block-quote";
import { CodeBlock } from "@/components/mdx/code-block";
import {
  Code,
  ExampleTabs,
  Implementation,
  Schema,
} from "@/components/mdx/example-tabs";
import { InlineCode } from "@/components/mdx/inline-code";
import { PackageInstallation } from "@/components/mdx/package-installation";
import { Video } from "@/components/mdx/video";
import { CookieConsent, Promo } from "@/components/misc";
import { GlobalStyle } from "@/style";
import { Main } from "./main";

export interface SiteLayoutProps {
  readonly children: ReactNode;
  readonly disableStars?: boolean;
}

export const SiteLayout: FC<SiteLayoutProps> = ({ children, disableStars }) => {
  const components = {
    pre: CodeBlock,
    inlineCode: InlineCode,
    blockquote: BlockQuote,
    ExampleTabs,
    Code,
    Implementation,
    Schema,
    PackageInstallation,
    Video,
  };

  return (
    <>
      <GlobalStyle />
      <MDXProvider components={components}>
        <Main>{children}</Main>
      </MDXProvider>
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
        const x = ((this.x / this.z) * canvas.width) / 2 + canvas.width / 2;
        const y = ((this.y / this.z) * canvas.height) / 2 + canvas.height / 2;
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
