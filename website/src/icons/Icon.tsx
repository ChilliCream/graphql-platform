import { library } from "@fortawesome/fontawesome-svg-core";
import {
  faArrowRight,
  faArrowRightArrowLeft,
  faAward,
  faBars,
  faBlog,
  faBuilding,
  faBuildingCircleArrowRight,
  faBuildingLock,
  faCalendarDay,
  faChartLine,
  faCheck,
  faChevronDown,
  faCircleArrowDown,
  faCircleHalfStroke,
  faCirclePlay,
  faClipboardCheck,
  faCloud,
  faCodeBranch,
  faComments,
  faCookieBite,
  faCubes,
  faDiagramProject,
  faEllipsis,
  faEnvelope,
  faFileContract,
  faFileLines,
  faGear,
  faHandshake,
  faHandshakeAngle,
  faHashtag,
  faHeadset,
  faHouse,
  faHouseLaptop,
  faLaptop,
  faLayerGroup,
  faLifeRing,
  faLink,
  faList,
  faMagnifyingGlass,
  faMap,
  faMessage,
  faMobileScreen,
  faMountain,
  faNewspaper,
  faPlug,
  faRightLeft,
  faRobot,
  faRocket,
  faRoute,
  faSeedling,
  faServer,
  faShield,
  faShirt,
  faStar,
  faUsers,
  faWindowMaximize,
  faWrench,
  faX,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ComponentProps, ReactNode } from "react";

/**
 * Add icons to this object in order to use them in the app. This strategy helps
 * us keep the bundle size to a minimum.
 */
const USED_ICONS = {
  faArrowRight,
  faArrowRightArrowLeft,
  faAward,
  faBars,
  faBlog,
  faBuilding,
  faBuildingCircleArrowRight,
  faBuildingLock,
  faCalendarDay,
  faChartLine,
  faCheck,
  faChevronDown,
  faCircleArrowDown,
  faCircleHalfStroke,
  faCirclePlay,
  faClipboardCheck,
  faCloud,
  faCodeBranch,
  faComments,
  faCookieBite,
  faCubes,
  faDiagramProject,
  faEllipsis,
  faEnvelope,
  faFileContract,
  faFileLines,
  faGear,
  faHandshake,
  faHandshakeAngle,
  faHashtag,
  faHeadset,
  faHouse,
  faHouseLaptop,
  faLaptop,
  faLayerGroup,
  faLifeRing,
  faLink,
  faList,
  faWindowMaximize,
  faMagnifyingGlass,
  faMap,
  faMessage,
  faMobileScreen,
  faMountain,
  faNewspaper,
  faPlug,
  faRightLeft,
  faRobot,
  faRocket,
  faRoute,
  faSeedling,
  faServer,
  faShield,
  faShirt,
  faStar,
  faUsers,
  faWrench,
  faX,
} as const;

// Only import the icons we use to keep the bundle size minimal
library.add(...Object.values(USED_ICONS));

// This removes the "fa" prefix from keys
type KeyWithoutPrefix<K extends PropertyKey> = K extends `fa${infer P}`
  ? Uncapitalize<P>
  : K;

// This transforms a PascalCase key into a kebab-case key
type KebabCase<
  T extends string,
  A extends string = "",
> = T extends `${infer F}${infer R}`
  ? KebabCase<R, `${A}${F extends Lowercase<F> ? "" : "-"}${Lowercase<F>}`>
  : A;

// This type transforms keys like "faMountain" and "faMagnifyingGlass" into
// "mountain" and "magnifying-glass" respectively.
export type IconName = KebabCase<KeyWithoutPrefix<keyof typeof USED_ICONS>>;

export const ICON_NAMES = Object.keys(USED_ICONS).map((s) =>
  s
    .substring(2)
    .replace(/([a-z])([A-Z])/g, "$1-$2")
    .toLowerCase(),
) as IconName[];

interface IconElementProps extends Omit<
  ComponentProps<typeof FontAwesomeIcon>,
  "icon"
> {
  icon: IconName;
}

/**
 * Wrapper for the FontAwesomeIcon element.
 *
 * @example <Icon icon="check" />
 * @see https://fontawesome.com/search?q=check&s=solid&ic=free-collection
 */
export function Icon({ icon, ...rest }: IconElementProps): ReactNode {
  return <FontAwesomeIcon icon={["fas", icon]} {...rest} />;
}
