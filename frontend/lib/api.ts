const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export interface ChatResponse {
  sessionId: string;
  reply: string;
}

export async function sendChatMessage(message: string, sessionId: string | null): Promise<ChatResponse> {
  const res = await fetch(`${API_BASE_URL}/api/chat`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ sessionId, message }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Chat request failed (${res.status}): ${text}`);
  }

  return res.json();
}

export interface Appointment {
  id: number;
  providerId: number;
  providerName: string;
  slotId: number;
  startTime: string;
  endTime: string;
  customerName: string;
  customerEmail: string;
  status: string;
}

export async function fetchAppointments(): Promise<Appointment[]> {
  const res = await fetch(`${API_BASE_URL}/api/appointments`);
  if (!res.ok) throw new Error(`Failed to fetch appointments (${res.status})`);
  return res.json();
}

export interface Provider {
  id: number;
  name: string;
  specialty: string | null;
}

export async function fetchProviders(): Promise<Provider[]> {
  const res = await fetch(`${API_BASE_URL}/api/providers`);
  if (!res.ok) throw new Error(`Failed to fetch providers (${res.status})`);
  return res.json();
}

export interface Slot {
  id: number;
  providerId: number;
  startTime: string;
  endTime: string;
  isBooked: boolean;
}

export async function fetchSlots(providerId: number): Promise<Slot[]> {
  const res = await fetch(`${API_BASE_URL}/api/slots?providerId=${providerId}&onlyAvailable=true`);
  if (!res.ok) throw new Error(`Failed to fetch slots (${res.status})`);
  return res.json();
}

export async function bookAppointment(
  slotId: number,
  customerName: string,
  customerEmail: string,
): Promise<Appointment> {
  const res = await fetch(`${API_BASE_URL}/api/appointments`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ slotId, customerName, customerEmail }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Booking failed (${res.status}): ${text}`);
  }

  return res.json();
}
