import Link from "next/link";

export default function NotFound() {
  return (
    <>
      <h1>404</h1>
      <p>This page does not exist.</p>
      <p>
        <Link href="/">Go home</Link>
      </p>
    </>
  );
}
