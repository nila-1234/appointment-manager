"use client";

import { useEffect, useState } from "react";
import { Box, Flex, Text, Heading, Badge, Separator, Callout } from "@radix-ui/themes";
import { CheckCircledIcon, ChevronDownIcon, ChevronRightIcon } from "@radix-ui/react-icons";
import { Provider, Slot, fetchProviders, fetchSlots } from "@/lib/api";
import { SlotGrid } from "@/components/SlotGrid";
import { BookingDialog } from "@/components/BookingDialog";

export function ProvidersPanel() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [expandedProviderId, setExpandedProviderId] = useState<number | null>(null);
  const [slots, setSlots] = useState<Slot[]>([]);
  const [isLoadingSlots, setIsLoadingSlots] = useState(false);
  const [selectedSlot, setSelectedSlot] = useState<Slot | null>(null);
  const [justBooked, setJustBooked] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchProviders()
      .then(setProviders)
      .catch((err) => setError(err instanceof Error ? err.message : "Failed to load providers."));
  }, []);

  async function loadSlots(providerId: number) {
    setIsLoadingSlots(true);
    setError(null);
    try {
      setSlots(await fetchSlots(providerId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load slots.");
    } finally {
      setIsLoadingSlots(false);
    }
  }

  function handleToggleProvider(providerId: number) {
    if (expandedProviderId === providerId) {
      setExpandedProviderId(null);
      setSlots([]);
      return;
    }
    setExpandedProviderId(providerId);
    setJustBooked(false);
    loadSlots(providerId);
  }

  function handleBooked() {
    setSelectedSlot(null);
    setJustBooked(true);
    if (expandedProviderId !== null) loadSlots(expandedProviderId);
  }

  const expandedProvider = providers.find((p) => p.id === expandedProviderId) ?? null;

  return (
    <Flex direction="column" gap="4">
      <Heading size="5">Our providers</Heading>

      {error && (
        <Callout.Root color="red" size="1">
          <Callout.Text>{error}</Callout.Text>
        </Callout.Root>
      )}

      {justBooked && (
        <Callout.Root color="green" size="1">
          <Callout.Icon>
            <CheckCircledIcon />
          </Callout.Icon>
          <Callout.Text>Appointment booked! Check your email for confirmation.</Callout.Text>
        </Callout.Root>
      )}

      <Flex direction="column" gap="3">
        {providers.map((provider) => {
          const isExpanded = expandedProviderId === provider.id;
          return (
            <Box
              key={provider.id}
              onClick={() => handleToggleProvider(provider.id)}
              p="4"
              style={{
                cursor: "pointer",
                borderRadius: "var(--radius-4)",
                background: "var(--color-panel-solid)",
                boxShadow: "var(--shadow-3)",
              }}
            >
              <Flex justify="between" align="center">
                <Flex direction="column" gap="1">
                  <Text weight="medium">{provider.name}</Text>
                  {provider.specialty && (
                    <Badge color="gray" variant="soft" style={{ width: "fit-content" }}>
                      {provider.specialty}
                    </Badge>
                  )}
                </Flex>
                {isExpanded ? <ChevronDownIcon /> : <ChevronRightIcon />}
              </Flex>

              {isExpanded && (
                <>
                  <Separator my="3" size="4" />
                  <div onClick={(e) => e.stopPropagation()}>
                    {isLoadingSlots ? (
                      <Text size="2" color="gray">
                        Loading availability…
                      </Text>
                    ) : (
                      <SlotGrid slots={slots} onSelect={setSelectedSlot} />
                    )}
                  </div>
                </>
              )}
            </Box>
          );
        })}
      </Flex>

      <BookingDialog
        slot={selectedSlot}
        providerName={expandedProvider?.name ?? ""}
        onClose={() => setSelectedSlot(null)}
        onBooked={handleBooked}
      />
    </Flex>
  );
}
