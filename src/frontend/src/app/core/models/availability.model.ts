export interface TimeSlot {
  start: string;
  end: string;
  isAvailable: boolean;
}

export interface AvailabilityResponse {
  date: string;
  availableSlots: TimeSlot[];
}
