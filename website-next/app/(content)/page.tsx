import Link from "next/link";

export default function Home() {
  return (
    <ul>
      <li>
        <Link href="/docs/fusion">Fusion</Link>
      </li>
      <li>
        <Link href="/docs/hotchocolate">Hot Chocolate</Link>
      </li>
      <li>
        <Link href="/docs/mocha">Mocha</Link>
      </li>
      <li>
        <Link href="/docs/nitro">Nitro</Link>
      </li>
      <li>
        <Link href="/docs/strawberryshake">Strawberry Shake</Link>
      </li>
      <li>
        <Link href="/blogs/some-blog">Some blog</Link>
      </li>
    </ul>
  );
}
