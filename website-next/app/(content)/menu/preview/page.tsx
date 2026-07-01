import type { Metadata } from "next";
import { MenuFlyoutGallery } from "@/src/components/header/previews/modals/MenuFlyoutGallery";

export const metadata: Metadata = {
  title: "Menu Flyout Modals",
  description: "Innovative variations of the navigation flyout panel.",
  robots: { index: false, follow: false },
};

export default function MenuFlyoutPreviewPage() {
  return <MenuFlyoutGallery />;
}
