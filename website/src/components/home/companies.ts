import type { CSSProperties } from "react";
import { AdditivLogo } from "@/src/icons/customer/AdditivLogo";
import { AeiLogo } from "@/src/icons/customer/AeiLogo";
import { AtminaLogo } from "@/src/icons/customer/AtminaLogo";
import { AutoguruLogo } from "@/src/icons/customer/AutoguruLogo";
import { BdnaLogo } from "@/src/icons/customer/BdnaLogo";
import { BeyableLogo } from "@/src/icons/customer/BeyableLogo";
import { BiqhLogo } from "@/src/icons/customer/BiqhLogo";
import { CarmmunityLogo } from "@/src/icons/customer/CarmmunityLogo";
import { CompassLogo } from "@/src/icons/customer/CompassLogo";
import { E2mLogo } from "@/src/icons/customer/E2mLogo";
import { ExlrtLogo } from "@/src/icons/customer/ExlrtLogo";
import { EzeepLogo } from "@/src/icons/customer/EzeepLogo";
import { FulcrumLogo } from "@/src/icons/customer/FulcrumLogo";
import { GalaxusLogo } from "@/src/icons/customer/GalaxusLogo";
import { GiaLogo } from "@/src/icons/customer/GiaLogo";
import { HiloLogo } from "@/src/icons/customer/HiloLogo";
import { IncloudLogo } from "@/src/icons/customer/IncloudLogo";
import { InfoslipsLogo } from "@/src/icons/customer/InfoslipsLogo";
import { MicrosoftLogo } from "@/src/icons/customer/MicrosoftLogo";
import { MotiviewLogo } from "@/src/icons/customer/MotiviewLogo";
import { OrderinLogo } from "@/src/icons/customer/OrderinLogo";
import { PoweredSoftLogo } from "@/src/icons/customer/PoweredSoftLogo";
import { PushpayLogo } from "@/src/icons/customer/PushpayLogo";
import { RailCargoAustriaLogo } from "@/src/icons/customer/RailCargoAustriaLogo";
import { Seven2OneLogo } from "@/src/icons/customer/Seven2OneLogo";
import { SolyticLogo } from "@/src/icons/customer/SolyticLogo";
import { SpectrumMedicalLogo } from "@/src/icons/customer/SpectrumMedicalLogo";
import { SpeedwayMotorsLogo } from "@/src/icons/customer/SpeedwayMotorsLogo";
import { SplashbackLogo } from "@/src/icons/customer/SplashbackLogo";
import { SweetGeeksLogo } from "@/src/icons/customer/SweetGeeksLogo";
import { SwissLifeLogo } from "@/src/icons/customer/SwissLifeLogo";
import { SytadelleLogo } from "@/src/icons/customer/SytadelleLogo";
import { TrackmanLogo } from "@/src/icons/customer/TrackmanLogo";
import { TravelSoftLogo } from "@/src/icons/customer/TravelSoftLogo";
import { VptechLogo } from "@/src/icons/customer/VptechLogo";
import { XMLogo } from "@/src/icons/customer/XMLogo";
import { ZioskLogo } from "@/src/icons/customer/ZioskLogo";

export interface Company {
  readonly name: string;
  readonly href: string;
  readonly Logo: (props: {
    readonly className?: string;
    readonly style?: CSSProperties;
  }) => React.JSX.Element;
  /** Target width in px; height follows from the logo's aspect ratio. */
  readonly width: number;
  /**
   * Optional max-height override for stacked/tall logos that the default
   * wordmark cap would otherwise shrink too much.
   */
  readonly maxHeightClassName?: string;
}

/**
 * The three anchor companies shown first (in order) before the rotation kicks
 * in. Keep these at the head of the list so the initial, server-rendered band
 * is deterministic and hydration-safe.
 */
export const FEATURED_COMPANIES: readonly Company[] = [
  {
    name: "Galaxus",
    href: "https://www.galaxus.ch",
    Logo: GalaxusLogo,
    width: 200,
  },
  {
    name: "Swiss Life",
    href: "https://www.swisslife.ch",
    Logo: SwissLifeLogo,
    width: 90,
    maxHeightClassName: "max-h-16",
  },
  {
    name: "Microsoft",
    href: "https://www.microsoft.com",
    Logo: MicrosoftLogo,
    width: 180,
  },
];

/** Every other customer, cycled through the band after the featured three. */
export const OTHER_COMPANIES: readonly Company[] = [
  {
    name: "additiv",
    href: "https://additiv.com",
    Logo: AdditivLogo,
    width: 140,
  },
  { name: "AEI", href: "https://aeieng.com", Logo: AeiLogo, width: 160 },
  { name: "ATMINA", href: "https://atmina.de", Logo: AtminaLogo, width: 120 },
  {
    name: "AutoGuru",
    href: "https://www.autoguru.com.au",
    Logo: AutoguruLogo,
    width: 180,
  },
  { name: "BDNA", href: "https://bdna.com.au", Logo: BdnaLogo, width: 150 },
  {
    name: "Beyable",
    href: "https://www.beyable.com",
    Logo: BeyableLogo,
    width: 150,
  },
  { name: "BIQH", href: "https://www.biqh.com", Logo: BiqhLogo, width: 110 },
  {
    name: "Carmmunity",
    href: "https://carmmunity.io",
    Logo: CarmmunityLogo,
    width: 180,
  },
  {
    name: "Compass Education",
    href: "https://www.compass.education",
    Logo: CompassLogo,
    width: 180,
  },
  { name: "e2m", href: "https://www.e2m.energy", Logo: E2mLogo, width: 100 },
  { name: "Exlrt", href: "https://www.exlrt.com", Logo: ExlrtLogo, width: 130 },
  { name: "ezeep", href: "https://www.ezeep.com", Logo: EzeepLogo, width: 110 },
  {
    name: "Fulcrum",
    href: "https://fulcrumpro.com/",
    Logo: FulcrumLogo,
    width: 150,
  },
  { name: "GIA", href: "https://gia.ch", Logo: GiaLogo, width: 120 },
  {
    name: "Hilo Energie",
    href: "https://www.hiloenergie.com",
    Logo: HiloLogo,
    width: 80,
  },
  {
    name: "Incloud",
    href: "https://www.incloud.de",
    Logo: IncloudLogo,
    width: 200,
  },
  {
    name: "Infoslips",
    href: "https://www.infoslips.com",
    Logo: InfoslipsLogo,
    width: 140,
  },
  {
    name: "Motitech",
    href: "https://motitech.co.uk",
    Logo: MotiviewLogo,
    width: 160,
  },
  {
    name: "Orderin",
    href: "https://orderin.co.za",
    Logo: OrderinLogo,
    width: 160,
  },
  {
    name: "PoweredSoft",
    href: "https://poweredsoft.com",
    Logo: PoweredSoftLogo,
    width: 130,
  },
  {
    name: "Pushpay",
    href: "https://pushpay.com",
    Logo: PushpayLogo,
    width: 180,
  },
  {
    name: "Rail Cargo Austria",
    href: "http://www.railcargo.at",
    Logo: RailCargoAustriaLogo,
    width: 240,
  },
  {
    name: "Seven2One",
    href: "https://www.seven2one.de",
    Logo: Seven2OneLogo,
    width: 130,
  },
  {
    name: "Solytic",
    href: "https://www.solytic.com",
    Logo: SolyticLogo,
    width: 150,
  },
  {
    name: "Spectrum Medical",
    href: "https://www.spectrummedical.com/",
    Logo: SpectrumMedicalLogo,
    width: 200,
  },
  {
    name: "Speedway Motors",
    href: "https://www.speedwaymotors.com",
    Logo: SpeedwayMotorsLogo,
    width: 130,
  },
  {
    name: "Splashback",
    href: "https://splashback.io",
    Logo: SplashbackLogo,
    width: 180,
  },
  {
    name: "Sweet Geeks",
    href: "https://sweetgeeks.dk",
    Logo: SweetGeeksLogo,
    width: 130,
  },
  {
    name: "Sytadelle",
    href: "https://www.sytadelle.fr",
    Logo: SytadelleLogo,
    width: 160,
  },
  {
    name: "TrackMan",
    href: "http://trackman.com/",
    Logo: TrackmanLogo,
    width: 180,
  },
  {
    name: "TravelSoft",
    href: "https://travel-soft.com",
    Logo: TravelSoftLogo,
    width: 180,
  },
  {
    name: "VPTech",
    href: "https://careers.veepee.com/vptech/",
    Logo: VptechLogo,
    width: 140,
  },
  { name: "XM", href: "https://xm.com", Logo: XMLogo, width: 120 },
  { name: "Ziosk", href: "https://www.ziosk.com", Logo: ZioskLogo, width: 120 },
];

export const ALL_COMPANIES: readonly Company[] = [
  ...FEATURED_COMPANIES,
  ...OTHER_COMPANIES,
];
