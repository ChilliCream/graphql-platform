type SwirlProps = {
  className?: string;
  style?: React.CSSProperties;
};

/**
 * The little four-blade "swirl" mark from the startpage artwork (the artistic
 * element that also reads as the header icon). Drawn with `currentColor` so it
 * can be tinted per placement; used purely as decoration on the landing hero.
 */
export function Swirl({ className, style }: SwirlProps) {
  return (
    <svg
      viewBox="9 139 35 35"
      fill="currentColor"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <path d="M10.75,146.77c-.29-.57-1.22-2.41-.45-3.62.78-1.22,2.87-1.15,3.39-1.13,6.49.23,9.41,11.16,13.33,10.62,3.76-.52,3.15-10.83,8.59-12.43,2.78-.82,6.45.85,6.78,2.71.66,3.77-12.67,6.53-12.65,12.2.01,4.51,8.46,6.29,8.36,11.98-.03,2.04-1.16,4.36-2.49,4.52-3.32.4-6.27-13.04-9.26-12.65-2.78.36-1.71,12.13-6.78,13.78-1.8.59-4.29-.13-4.97-1.58-1.88-3.99,11.01-11.11,9.26-16.04-1.36-3.83-10.19-2.62-13.11-8.36Z" />
    </svg>
  );
}
