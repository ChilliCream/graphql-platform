import Link from "next/link";

export default function Home() {
  return (
    <>
      <h2>Docs</h2>
      <ul>
        <li>
          <Link href="/docs/example/getting-started">Example</Link>
        </li>
      </ul>
    </>
  );
}
