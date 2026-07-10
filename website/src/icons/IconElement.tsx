import {
  IconDefinition,
  IconLookup,
  IconName,
  byPrefixAndName,
} from "@awesome.me/kit-04be88f754/icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ComponentProps, ReactNode } from "react";

declare module "@fortawesome/fontawesome-svg-core" {
  export function icon(icon: IconName | IconLookup, params?: IconParams): Icon;
  export function findIconDefinition(iconLookup: IconLookup): IconDefinition;
}

// TODO implement a fallback for tokenless development since this is a public
// repo
const FREE_NAMESPACE = "todo";

const STYLE_MAPPING = {
  classic: "far",
  duotone: "fadr",
  duolight: "fadl",
} as const;

interface IconElementProps extends Omit<
  ComponentProps<typeof FontAwesomeIcon>,
  "icon"
> {
  icon: IconName;
  variant?: keyof typeof STYLE_MAPPING;
}

export function IconElement({
  icon,
  variant = "classic",
  ...rest
}: IconElementProps): ReactNode {
  const namespace = STYLE_MAPPING[variant];
  return <FontAwesomeIcon icon={byPrefixAndName[namespace][icon]} {...rest} />;
}
