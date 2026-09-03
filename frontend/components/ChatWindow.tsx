"use client";

import { useEffect, useRef, useState } from "react";
import { Box, Flex, Heading, Text, TextField, Button, Callout } from "@radix-ui/themes";
import { ChatBubbleIcon, InfoCircledIcon, PaperPlaneIcon } from "@radix-ui/react-icons";
import { sendChatMessage } from "@/lib/api";
import { ChatMessage, MessageBubble } from "@/components/MessageBubble";

const SESSION_STORAGE_KEY = "appointment-manager-session-id";

const WELCOME_MESSAGE: ChatMessage = {
  role: "assistant",
  content:
    "Hi! I can help you check provider availability, book, reschedule, or cancel an appointment. Who would you like to see, or what would you like to do?",
};

export function ChatWindow() {
  const [messages, setMessages] = useState<ChatMessage[]>([WELCOME_MESSAGE]);
  const [input, setInput] = useState("");
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const scrollRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    setSessionId(sessionStorage.getItem(SESSION_STORAGE_KEY));
  }, []);

  useEffect(() => {
    scrollRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  async function handleSend() {
    const trimmed = input.trim();
    if (!trimmed || isSending) return;

    setMessages((prev) => [...prev, { role: "user", content: trimmed }]);
    setInput("");
    setIsSending(true);
    setError(null);

    try {
      const response = await sendChatMessage(trimmed, sessionId);
      setSessionId(response.sessionId);
      sessionStorage.setItem(SESSION_STORAGE_KEY, response.sessionId);
      setMessages((prev) => [...prev, { role: "assistant", content: response.reply }]);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong.");
    } finally {
      setIsSending(false);
    }
  }

  return (
    <Flex
      direction="column"
      height="100%"
      style={{
        borderRadius: "var(--radius-4)",
        background: "var(--color-panel-solid)",
        boxShadow: "var(--shadow-4)",
        overflow: "hidden",
      }}
    >
      <Flex align="center" gap="2" px="4" py="3" style={{ borderBottom: "1px solid var(--gray-a5)" }}>
        <ChatBubbleIcon />
        <Heading size="3">Assistant</Heading>
      </Flex>

      <Box flexGrow="1" p="4" style={{ overflowY: "auto", background: "var(--gray-a2)" }}>
        <Flex direction="column" gap="3">
          {messages.map((m, i) => (
            <MessageBubble key={i} message={m} />
          ))}
          {isSending && (
            <Box style={{ display: "flex", justifyContent: "flex-start" }}>
              <Box px="3" py="2" style={{ background: "var(--gray-a3)", borderRadius: "var(--radius-4)" }}>
                <Text size="2" color="gray">
                  Thinking…
                </Text>
              </Box>
            </Box>
          )}
          {error && (
            <Callout.Root color="red" size="1">
              <Callout.Icon>
                <InfoCircledIcon />
              </Callout.Icon>
              <Callout.Text>{error}</Callout.Text>
            </Callout.Root>
          )}
          <div ref={scrollRef} />
        </Flex>
      </Box>

      <Flex gap="2" p="3" style={{ borderTop: "1px solid var(--gray-a5)" }}>
        <TextField.Root
          style={{ flexGrow: 1 }}
          placeholder="Type a message…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") handleSend();
          }}
          disabled={isSending}
        />
        <Button onClick={handleSend} disabled={isSending || !input.trim()}>
          <PaperPlaneIcon />
          Send
        </Button>
      </Flex>
    </Flex>
  );
}
