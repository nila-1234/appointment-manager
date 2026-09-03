"use client";

import { useState } from "react";
import { Dialog, Flex, Text, TextField, Button, Callout } from "@radix-ui/themes";
import { InfoCircledIcon } from "@radix-ui/react-icons";
import { Slot, bookAppointment } from "@/lib/api";

const timeFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: "long",
  month: "long",
  day: "numeric",
  hour: "numeric",
  minute: "2-digit",
});

export function BookingDialog({
  slot,
  providerName,
  onClose,
  onBooked,
}: {
  slot: Slot | null;
  providerName: string;
  onClose: () => void;
  onBooked: () => void;
}) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!slot || !name.trim() || !email.trim()) return;

    setIsSubmitting(true);
    setError(null);
    try {
      await bookAppointment(slot.id, name.trim(), email.trim());
      setName("");
      setEmail("");
      onBooked();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Something went wrong.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog.Root open={slot !== null} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Content maxWidth="420px">
        <Dialog.Title>Confirm your appointment</Dialog.Title>
        <Dialog.Description size="2" color="gray" mb="4">
          {slot ? `${providerName} — ${timeFormatter.format(new Date(slot.startTime))}` : ""}
        </Dialog.Description>

        <form onSubmit={handleSubmit}>
          <Flex direction="column" gap="3">
            <label>
              <Text as="div" size="2" weight="medium" mb="1">
                Your name
              </Text>
              <TextField.Root
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Jane Doe"
                required
              />
            </label>
            <label>
              <Text as="div" size="2" weight="medium" mb="1">
                Email
              </Text>
              <TextField.Root
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="jane@example.com"
                required
              />
            </label>

            {error && (
              <Callout.Root color="red" size="1">
                <Callout.Icon>
                  <InfoCircledIcon />
                </Callout.Icon>
                <Callout.Text>{error}</Callout.Text>
              </Callout.Root>
            )}

            <Flex gap="3" justify="end" mt="2">
              <Dialog.Close>
                <Button type="button" variant="soft" color="gray" disabled={isSubmitting}>
                  Cancel
                </Button>
              </Dialog.Close>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting ? "Booking…" : "Confirm booking"}
              </Button>
            </Flex>
          </Flex>
        </form>
      </Dialog.Content>
    </Dialog.Root>
  );
}
