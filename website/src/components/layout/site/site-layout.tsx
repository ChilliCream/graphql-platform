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

/**
 * There are three different implementations for the Stars component. The first
 * one does bareely change the code from the original implementation but uses
 * more CPU. Then there are two performance optimized implementations that use
 * a grid or a quadtree to connect nearby stars which is much more efficent.
 */
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
    const connectionDistance = 100; // Maximum distance for connecting stars
    let stars: Star[] = [];

    function setCanvasSize() {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    }

    class Star {
      constructor(
        public x: number,
        public y: number,
        public z: number,
        public size: number
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

      getScreenPosition() {
        return {
          x: ((this.x / this.z) * canvas.width) / 2 + canvas.width / 2,
          y: ((this.y / this.z) * canvas.height) / 2 + canvas.height / 2,
        };
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
        connectNearbyStars();
      }
    }

    /**
     * This is a very simple implementation to connect nearby stars. It's a bit
     * CPU intensive. There is a more efficient way listed in the commented out
     * code below, but it's a bit more complex but does barely use any CPU even
     * without a FPS limit.
     */
    function connectNearbyStars() {
      if (!ctx) return;

      ctx.strokeStyle = "rgba(244, 235, 203, 0.2)"; // Light, semi-transparent color for lines
      ctx.lineWidth = 0.5;

      for (let i = 0; i < stars.length; i++) {
        const star1 = stars[i];
        const pos1 = star1.getScreenPosition();

        for (let j = i + 1; j < stars.length; j++) {
          const star2 = stars[j];
          const pos2 = star2.getScreenPosition();

          const distance = Math.sqrt(
            Math.pow(pos1.x - pos2.x, 2) + Math.pow(pos1.y - pos2.y, 2)
          );

          if (distance < connectionDistance) {
            ctx.beginPath();
            ctx.moveTo(pos1.x, pos1.y);
            ctx.lineTo(pos2.x, pos2.y);
            ctx.stroke();
          }
        }
      }
    }

    // in this implementation it's better to limit the FPS as the connection
    // calculation is a CPU intensive.
    let lastTime = 0;
    const fps = 30;
    const fpsInterval = 1000 / fps;

    function animate(currentTime: number) {
      requestAnimationFrame(animate);

      // Calculate elapsed time since last frame
      const elapsed = currentTime - lastTime;

      // Proceed only if enough time has passed to maintain the desired FPS
      if (elapsed > fpsInterval) {
        lastTime = currentTime - (elapsed % fpsInterval);

        updateStars();
        drawStars();
      }
    }

    window.addEventListener("resize", () => {
      setCanvasSize();
      initStars();
    });

    setCanvasSize();
    initStars();
    requestAnimationFrame(animate);
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

/**
 * This is a more efficient way to connect nearby stars. It uses a grid to
 * divide the canvas into cells and only checks stars in neighboring cells.
 * This is more efficient than the brute force method above, but it's a bit
 * more complex.
 *
 * This could be probably optimized further by using a quadtree, but that's
 * out of the scope at the moment
 */
// function Stars(): ReactElement {
//   const canvasRef = useRef<HTMLCanvasElement>(null);

//   useLayoutEffect(() => {
//     if (!canvasRef.current) {
//       return;
//     }

//     const canvas = canvasRef.current;
//     const ctx = canvas.getContext("2d");
//     const numStars = 800;
//     const speed = 0.05;
//     const connectionDistance = 100;
//     const cellSize = connectionDistance;
//     let stars: Star[] = [];
//     let grid: Star[][][] = [];

//     function setCanvasSize() {
//       canvas.width = window.innerWidth;
//       canvas.height = window.innerHeight;
//     }

//     class Star {
//       gridX: number = 0;
//       gridY: number = 0;

//       constructor(
//         public x: number,
//         public y: number,
//         public z: number,
//         public size: number
//       ) {}

//       update() {
//         this.z -= speed;
//         if (this.z <= 0) {
//           this.reset();
//         }
//       }

//       reset() {
//         this.z = canvas.width;
//         this.x = Math.random() * (canvas.width * 2) - canvas.width;
//         this.y = Math.random() * (canvas.height * 2) - canvas.height;
//         this.size = Math.random() * 2 + 1;
//       }

//       draw() {
//         const x = ((this.x / this.z) * canvas.width) / 2 + canvas.width / 2;
//         const y = ((this.y / this.z) * canvas.height) / 2 + canvas.height / 2;
//         const radius = (1 - this.z / canvas.width) * this.size;

//         if (ctx) {
//           ctx.beginPath();
//           ctx.arc(x, y, radius, 0, Math.PI * 2);
//           ctx.fill();
//         }

//         this.gridX = Math.floor(x / cellSize);
//         this.gridY = Math.floor(y / cellSize);
//       }

//       getScreenPosition() {
//         return {
//           x: ((this.x / this.z) * canvas.width) / 2 + canvas.width / 2,
//           y: ((this.y / this.z) * canvas.height) / 2 + canvas.height / 2,
//         };
//       }
//     }

//     function initStars() {
//       stars = Array.from({ length: numStars }, () => {
//         const star = new Star(
//           Math.random() * (canvas.width * 2) - canvas.width,
//           Math.random() * (canvas.height * 2) - canvas.height,
//           Math.random() * canvas.width,
//           Math.random() * 2 + 1
//         );
//         return star;
//       });
//     }

//     function updateStars() {
//       stars.forEach((star) => star.update());
//     }

//     function drawStars() {
//       if (ctx) {
//         ctx.clearRect(0, 0, canvas.width, canvas.height);
//         ctx.fillStyle = "#f4ebcb";
//         stars.forEach((star) => star.draw());
//         connectNearbyStars();
//       }
//     }

//     function buildGrid() {
//       /**
//        * Spatial partitioning using a grid to optimize performance:
//        * - Reduces the number of distance calculations by only checking stars within nearby cells.
//        * - Converts O(n²) complexity to approximately O(n) for connecting stars.
//        * - Cell size is set to connectionDistance to ensure all possible connections are considered.
//        */
//       const cols = Math.ceil(canvas.width / cellSize);
//       const rows = Math.ceil(canvas.height / cellSize);
//       grid = Array(cols)
//         .fill(null)
//         .map(() =>
//           Array(rows)
//             .fill(null)
//             .map(() => [])
//         );

//       stars.forEach((star) => {
//         const { x, y } = star.getScreenPosition();
//         const gridX = Math.floor(x / cellSize);
//         const gridY = Math.floor(y / cellSize);
//         if (grid[gridX] && grid[gridX][gridY]) {
//           grid[gridX][gridY].push(star);
//         }
//       });
//     }

//     function connectNearbyStars() {
//       if (!ctx) return;

//       buildGrid();

//       ctx.strokeStyle = "rgba(244, 235, 203, 0.2)";
//       ctx.lineWidth = 0.5;

//       const cols = grid.length;
//       const rows = grid[0].length;

//       for (let i = 0; i < cols; i++) {
//         for (let j = 0; j < rows; j++) {
//           const cellStars = grid[i][j];
//           for (let k = 0; k < cellStars.length; k++) {
//             const star1 = cellStars[k];
//             const pos1 = star1.getScreenPosition();

//             // Check neighboring cells
//             for (let dx = -1; dx <= 1; dx++) {
//               for (let dy = -1; dy <= 1; dy++) {
//                 const ni = i + dx;
//                 const nj = j + dy;
//                 if (ni >= 0 && ni < cols && nj >= 0 && nj < rows) {
//                   const neighborStars = grid[ni][nj];
//                   for (let l = 0; l < neighborStars.length; l++) {
//                     const star2 = neighborStars[l];
//                     if (star1 === star2) continue;

//                     const pos2 = star2.getScreenPosition();
//                     const distance = Math.hypot(
//                       pos1.x - pos2.x,
//                       pos1.y - pos2.y
//                     );

//                     if (distance < connectionDistance) {
//                       ctx.beginPath();
//                       ctx.moveTo(pos1.x, pos1.y);
//                       ctx.lineTo(pos2.x, pos2.y);
//                       ctx.stroke();
//                     }
//                   }
//                 }
//               }
//             }
//           }
//         }
//       }
//     }

//     function animate() {
//       updateStars();
//       drawStars();
//       requestAnimationFrame(animate);
//     }

//     window.addEventListener("resize", () => {
//       setCanvasSize();
//       initStars();
//     });

//     setCanvasSize();
//     initStars();
//     animate();
//   }, []);

//   return (
//     <canvas
//       ref={canvasRef}
//       style={{
//         position: "absolute",
//         top: 0,
//         right: 0,
//         bottom: 0,
//         left: 0,
//         zIndex: -2,
//         width: "100vw",
//         height: "100vh",
//       }}
//     />
//   );
// }

/**
 * This implementation uses a Quadtree to optimize the connection of nearby
 * stars. It's similar in complexity to the grid implementation but is more
 * efficient and uses less CPU.
 */
// function Stars(): ReactElement {
//   const canvasRef = useRef<HTMLCanvasElement>(null);

//   useLayoutEffect(() => {
//     if (!canvasRef.current) {
//       return;
//     }

//     const canvas = canvasRef.current;
//     const ctx = canvas.getContext("2d");
//     const numStars = 800;
//     const speed = 0.25;
//     const connectionDistance = 100; // Maximum distance for connecting stars
//     let stars: Star[] = [];

//     function setCanvasSize() {
//       canvas.width = window.innerWidth;
//       canvas.height = window.innerHeight;
//     }

//     class Star {
//       screenX: number = 0;
//       screenY: number = 0;

//       constructor(
//         public x: number,
//         public y: number,
//         public z: number,
//         public size: number
//       ) {}

//       update() {
//         this.z -= speed;
//         if (this.z <= 0) {
//           this.reset();
//         }
//       }

//       reset() {
//         this.z = canvas.width;
//         this.x = Math.random() * (canvas.width * 2) - canvas.width;
//         this.y = Math.random() * (canvas.height * 2) - canvas.height;
//         this.size = Math.random() * 2 + 1;
//       }

//       draw() {
//         const x = ((this.x / this.z) * canvas.width) / 2 + canvas.width / 2;
//         const y = ((this.y / this.z) * canvas.height) / 2 + canvas.height / 2;
//         const radius = (1 - this.z / canvas.width) * this.size;

//         this.screenX = x;
//         this.screenY = y;

//         if (ctx) {
//           ctx.beginPath();
//           ctx.arc(x, y, radius, 0, Math.PI * 2);
//           ctx.fill();
//         }
//       }
//     }

//     function initStars() {
//       stars = Array.from(
//         { length: numStars },
//         () =>
//           new Star(
//             Math.random() * (canvas.width * 2) - canvas.width,
//             Math.random() * (canvas.height * 2) - canvas.height,
//             Math.random() * canvas.width,
//             Math.random() * 2 + 1
//           )
//       );
//     }

//     function updateStars() {
//       stars.forEach((star) => star.update());
//     }

//     function drawStars() {
//       if (ctx) {
//         ctx.clearRect(0, 0, canvas.width, canvas.height);
//         ctx.fillStyle = "#f4ebcb";
//         stars.forEach((star) => star.draw());
//         connectNearbyStars();
//       }
//     }

//     function connectNearbyStars() {
//       if (!ctx) return;

//       // Build the Quadtree
//       const boundary = new Rectangle(
//         canvas.width / 2,
//         canvas.height / 2,
//         canvas.width / 2,
//         canvas.height / 2
//       );
//       const qt = new Quadtree(boundary, 4);

//       // Insert stars into the Quadtree
//       stars.forEach((star) => {
//         const point = new Point(star.screenX, star.screenY, star);
//         qt.insert(point);
//       });

//       ctx.strokeStyle = "rgba(244, 235, 203, 0.2)";
//       ctx.lineWidth = 0.5;

//       // For each star, find nearby stars using the Quadtree
//       stars.forEach((star) => {
//         const range = new Rectangle(
//           star.screenX,
//           star.screenY,
//           connectionDistance,
//           connectionDistance
//         );
//         const points = qt.query(range);

//         points.forEach((point) => {
//           if (point.userData !== star) {
//             ctx.beginPath();
//             ctx.moveTo(star.screenX, star.screenY);
//             ctx.lineTo(point.x, point.y);
//             ctx.stroke();
//           }
//         });
//       });
//     }

//     // Quadtree implementation
//     class Point {
//       constructor(
//         public x: number,
//         public y: number,
//         public userData: any = null
//       ) {}
//     }

//     class Rectangle {
//       constructor(
//         public x: number,
//         public y: number,
//         public w: number,
//         public h: number
//       ) {}

//       contains(point: Point) {
//         return (
//           point.x >= this.x - this.w &&
//           point.x <= this.x + this.w &&
//           point.y >= this.y - this.h &&
//           point.y <= this.y + this.h
//         );
//       }

//       intersects(range: Rectangle) {
//         return !(
//           range.x - range.w > this.x + this.w ||
//           range.x + range.w < this.x - this.w ||
//           range.y - range.h > this.y + this.h ||
//           range.y + range.h < this.y - this.h
//         );
//       }
//     }

//     class Quadtree {
//       points: Point[] = [];
//       divided: boolean = false;
//       northeast: Quadtree | null = null;
//       northwest: Quadtree | null = null;
//       southeast: Quadtree | null = null;
//       southwest: Quadtree | null = null;

//       constructor(public boundary: Rectangle, public capacity: number) {}

//       subdivide() {
//         const { x, y, w, h } = this.boundary;
//         const ne = new Rectangle(x + w / 2, y - h / 2, w / 2, h / 2);
//         this.northeast = new Quadtree(ne, this.capacity);
//         const nw = new Rectangle(x - w / 2, y - h / 2, w / 2, h / 2);
//         this.northwest = new Quadtree(nw, this.capacity);
//         const se = new Rectangle(x + w / 2, y + h / 2, w / 2, h / 2);
//         this.southeast = new Quadtree(se, this.capacity);
//         const sw = new Rectangle(x - w / 2, y + h / 2, w / 2, h / 2);
//         this.southwest = new Quadtree(sw, this.capacity);
//         this.divided = true;
//       }

//       insert(point: Point): boolean {
//         if (!this.boundary.contains(point)) {
//           return false;
//         }
//         if (this.points.length < this.capacity) {
//           this.points.push(point);
//           return true;
//         } else {
//           if (!this.divided) {
//             this.subdivide();
//           }
//           if (this.northeast!.insert(point)) return true;
//           if (this.northwest!.insert(point)) return true;
//           if (this.southeast!.insert(point)) return true;
//           if (this.southwest!.insert(point)) return true;
//         }
//         return false;
//       }

//       query(range: Rectangle, found: Point[] = []): Point[] {
//         if (!this.boundary.intersects(range)) {
//           return found;
//         } else {
//           for (const p of this.points) {
//             if (range.contains(p)) {
//               found.push(p);
//             }
//           }
//           if (this.divided) {
//             this.northwest!.query(range, found);
//             this.northeast!.query(range, found);
//             this.southwest!.query(range, found);
//             this.southeast!.query(range, found);
//           }
//         }
//         return found;
//       }
//     }

//     // Limit FPS to improve performance
//     let lastTime = 0;
//     const fps = 30;
//     const fpsInterval = 1000 / fps;

//     function animate(currentTime: number) {
//       requestAnimationFrame(animate);

//       // Calculate elapsed time since last frame
//       const elapsed = currentTime - lastTime;

//       // Proceed only if enough time has passed to maintain the desired FPS
//       if (elapsed > fpsInterval) {
//         lastTime = currentTime - (elapsed % fpsInterval);

//         updateStars();
//         drawStars();
//       }
//     }

//     window.addEventListener("resize", () => {
//       setCanvasSize();
//       initStars();
//     });

//     setCanvasSize();
//     initStars();
//     requestAnimationFrame(animate);
//   }, []);

//   return (
//     <canvas
//       ref={canvasRef}
//       style={{
//         position: "absolute",
//         top: 0,
//         right: 0,
//         bottom: 0,
//         left: 0,
//         zIndex: -2,
//         width: "100vw",
//         height: "100vh",
//       }}
//     />
//   );
// }
