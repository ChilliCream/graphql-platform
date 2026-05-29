import "./globals.css";
import Footer from "@/src/components/Footer";
import Header from "@/src/components/Header";
import { SearchProvider } from "@/src/components/Search";

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <SearchProvider>
          <Header />
          <main>{children}</main>
          <Footer />
        </SearchProvider>
      </body>
    </html>
  );
}
