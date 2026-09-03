import { Box, Flex } from "@radix-ui/themes";
import { Header } from "@/components/Header";
import { ProvidersPanel } from "@/components/ProvidersPanel";
import { ChatWindow } from "@/components/ChatWindow";

export default function Home() {
  return (
    <Flex direction="column" style={{ minHeight: "100vh" }}>
      <Header />

      <Flex
        flexGrow="1"
        gap="6"
        p={{ initial: "4", sm: "6" }}
        direction={{ initial: "column", md: "row" }}
        style={{ maxWidth: "1200px", width: "100%", margin: "0 auto" }}
      >
        <Box flexGrow="1" flexBasis="0" minWidth="0">
          <ProvidersPanel />
        </Box>

        <Box
          flexShrink="0"
          width={{ initial: "100%", md: "380px" }}
          height={{ initial: "480px", md: "calc(100vh - 140px)" }}
          style={{ position: "sticky", top: "24px" }}
        >
          <ChatWindow />
        </Box>
      </Flex>
    </Flex>
  );
}
