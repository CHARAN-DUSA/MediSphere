export interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  specialty: string;
  qualification: string;
  experienceYears: number;
  consultationFee: number;
  profileImageUrl: string;
  bio: string;
  isAvailable: boolean;
  isApproved: boolean;
  isActive: boolean;
  approvalStatus: string;
  gender: string;
  location: string;
  languagesSpoken: string;
  averageRating: number;
  ratingCount: number;
  departmentId: number;
  departmentName: string;
}

export interface DoctorFilter {
  page?: number;
  pageSize?: number;
  specialty?: string;
  departmentId?: number;
  search?: string;
  gender?: string;
  location?: string;
  language?: string;
  minFee?: number;
  maxFee?: number;
  minRating?: number;
  isAvailable?: boolean;
}

export interface DoctorSchedule {
  id?: number;
  doctorId: number;
  dayOfWeek: number; // 0 = Sunday, 1 = Monday, etc.
  startTime: string; // "HH:mm:ss"
  endTime: string; // "HH:mm:ss"
  slotDurationMinutes: number;
  isActive: boolean;
}

export interface BlockSlotDto {
  date: string; // YYYY-MM-DD
  startTime: string; // "HH:mm:ss"
  reason: string;
}

export interface VacationDto {
  startDate: string; // YYYY-MM-DD
  endDate: string; // YYYY-MM-DD
  reason: string;
}

export interface DoctorEarningsDto {
  totalGrossEarnings: number;
  totalNetEarnings: number;
  totalPlatformFeesPaid: number;
  totalTaxesPaid: number;
  totalAdminCommissionPaid: number;
  paidAppointmentsCount: number;
}

