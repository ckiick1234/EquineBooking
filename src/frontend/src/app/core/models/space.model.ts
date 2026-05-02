export type SpaceType = 'Corral' | 'Stall';
export type SlotType = 'Hourly' | 'Daily';

export interface HourlyAvailability {
  start: string;
  end: string;
}

export interface Space {
  id: string;
  name: string;
  type: SpaceType;
  slotType: SlotType;
  description: string;
  isActive: boolean;
  hourlyAvailability?: HourlyAvailability | null;
  rules: string;
  blockedDates?: string[];
}
