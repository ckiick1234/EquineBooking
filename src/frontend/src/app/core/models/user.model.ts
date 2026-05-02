export type UserRole = 'Admin' | 'Client';

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  phone: string;
  role: UserRole;
  createdAt: string;
  isActive: boolean;
}

export interface UpdateUserProfileRequest {
  displayName?: string;
  phone?: string;
}
