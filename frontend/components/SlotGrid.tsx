import { Flex, Text, Button } from "@radix-ui/themes";
import { Slot } from "@/lib/api";

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  weekday: "short",
  month: "short",
  day: "numeric",
});

const timeFormatter = new Intl.DateTimeFormat(undefined, {
  hour: "numeric",
  minute: "2-digit",
});

export function SlotGrid({ slots, onSelect }: { slots: Slot[]; onSelect: (slot: Slot) => void }) {
  if (slots.length === 0) {
    return (
      <Text size="2" color="gray">
        No open slots right now — check back soon.
      </Text>
    );
  }

  const byDate = new Map<string, Slot[]>();
  for (const slot of slots) {
    const key = new Date(slot.startTime).toDateString();
    if (!byDate.has(key)) byDate.set(key, []);
    byDate.get(key)!.push(slot);
  }

  return (
    <Flex direction="column" gap="4">
      {Array.from(byDate.entries()).map(([dateKey, daySlots]) => (
        <Flex key={dateKey} direction="column" gap="2">
          <Text size="2" weight="medium" color="gray">
            {dateFormatter.format(new Date(daySlots[0].startTime))}
          </Text>
          <Flex wrap="wrap" gap="2">
            {daySlots.map((slot) => (
              <Button key={slot.id} variant="soft" size="2" onClick={() => onSelect(slot)}>
                {timeFormatter.format(new Date(slot.startTime))}
              </Button>
            ))}
          </Flex>
        </Flex>
      ))}
    </Flex>
  );
}
