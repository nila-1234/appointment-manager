import { Box, Text } from "@radix-ui/themes";

export interface ChatMessage {
  role: "user" | "assistant";
  content: string;
}

export function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === "user";

  return (
    <Box style={{ display: "flex", justifyContent: isUser ? "flex-end" : "flex-start" }}>
      <Box
        maxWidth="85%"
        px="3"
        py="2"
        style={{
          background: isUser ? "var(--accent-9)" : "var(--gray-a3)",
          color: isUser ? "white" : "inherit",
          borderRadius: "var(--radius-4)",
          borderBottomRightRadius: isUser ? "var(--radius-1)" : "var(--radius-4)",
          borderBottomLeftRadius: isUser ? "var(--radius-4)" : "var(--radius-1)",
        }}
      >
        <Text size="2" style={{ whiteSpace: "pre-wrap" }}>
          {message.content}
        </Text>
      </Box>
    </Box>
  );
}
