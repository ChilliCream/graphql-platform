import { type IconName, library } from "@fortawesome/fontawesome-svg-core";
import {
  faArrowRight,
  faArrowRightArrowLeft,
  faAward,
  faBars,
  faBlog,
  faBuilding,
  faBuildingLock,
  faChartLine,
  faChevronDown,
  faCirclePlay,
  faClipboardCheck,
  faCloud,
  faCodeBranch,
  faCookieBite,
  faCubes,
  faDiagramProject,
  faEllipsis,
  faEnvelope,
  faFileContract,
  faFileLines,
  faGear,
  faHandshakeAngle,
  faHashtag,
  faLayerGroup,
  faLink,
  faMessage,
  faNewspaper,
  faRightLeft,
  faRobot,
  faRocket,
  faRoute,
  faServer,
  faShirt,
  faStar,
} from "@fortawesome/free-solid-svg-icons";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { ComponentProps, ReactNode } from "react";

// Only import the icons we use to keep the bundle size minimal
library.add(
  faArrowRight,
  faArrowRightArrowLeft,
  faAward,
  faBars,
  faBlog,
  faBuilding,
  faBuildingLock,
  faChevronDown,
  faCirclePlay,
  faChartLine,
  faClipboardCheck,
  faCloud,
  faCodeBranch,
  faCookieBite,
  faCubes,
  faDiagramProject,
  faEllipsis,
  faEnvelope,
  faFileContract,
  faFileLines,
  faGear,
  faHandshakeAngle,
  faHashtag,
  faLayerGroup,
  faLink,
  faMessage,
  faNewspaper,
  faRightLeft,
  faRobot,
  faRocket,
  faRoute,
  faServer,
  faShirt,
  faStar,
);

interface IconElementProps extends Omit<
  ComponentProps<typeof FontAwesomeIcon>,
  "icon"
> {
  icon: IconName;
}

export function IconElement({ icon, ...rest }: IconElementProps): ReactNode {
  return <FontAwesomeIcon icon={["fas", icon]} {...rest} />;
}
