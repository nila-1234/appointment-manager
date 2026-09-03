import { Flex, Heading, Text, Box } from "@radix-ui/themes";
import { CalendarIcon } from "@radix-ui/react-icons";

export function Header() {
  return (
    <Box
      style={{
        background: "linear-gradient(120deg, var(--teal-9), var(--green-9))",
      }}
      px={{ initial: "4", sm: "6" }}
      py="4"
    >
      <Flex align="center" gap="3">
        <Flex
          align="center"
          justify="center"
          width="36px"
          height="36px"
          style={{ background: "rgba(255, 255, 255, 0.2)", borderRadius: "var(--radius-3)" }}
        >
          <CalendarIcon width={18} height={18} color="white" />
        </Flex>
        <Box>
          <Heading size="4" weight="bold" style={{ color: "white" }}>
            Appointment Manager
          </Heading>
          <Text size="1" style={{ color: "rgba(255, 255, 255, 0.85)" }}>
            Book time with our providers, or just ask the assistant
          </Text>
        </Box>
      </Flex>
    </Box>
  );
}
