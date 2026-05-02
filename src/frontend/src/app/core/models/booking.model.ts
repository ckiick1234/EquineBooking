export type BookingStatus =
  | 'Pending'
  | 'Approved'
  | 'Declined'
  | 'Cancelled'
  | 'ModificationRequested';

export interface Booking {
  id: string;
  spaceId: string;
  spaceName: string;
  userId: string;
  userEmail: string;
  userName: string;
  startTime: string;
  endTime: string;
  status: BookingStatus;
  notes: string;
  adminNotes: string;
  createdAt: string;
  updatedAt: string;
  venmoReminder: boolean;
}

export interface CreateBookingRequest {
  spaceId: string;
  startTime: string;
  endTime: string;
  notes?: string | null;
}

export interface UpdateBookingRequest {
  startTime?: string | null;
  endTime?: string | null;
  notes?: string | null;
}

export type AdminAction = 'Approve' | 'Decline';

export interface AdminActionRequest {
  action: AdminAction;
  adminNotes?: string | null;
}
