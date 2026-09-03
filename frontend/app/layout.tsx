import type { Metadata } from "next";
import { DM_Sans } from "next/font/google";
import { Theme } from "@radix-ui/themes";
import "@radix-ui/themes/styles.css";
import "./globals.css";

const dmSans = DM_Sans({
  variable: "--font-dm-sans",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Appointment Manager",
  description: "AI appointment booking assistant",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${dmSans.variable} h-full antialiased`}>
      <body className="min-h-full">
        <Theme accentColor="teal" radius="large" panelBackground="solid" hasBackground={false}>
          {children}
        </Theme>
      </body>
    </html>
  );
}
