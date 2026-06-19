export interface LoginDto { email: string; password: string; }

export interface RegisterPatientDto {
  firstName: string; lastName: string; email: string;
  password: string; phoneNumber: string; dateOfBirth: string;
  gender: string; address: string; bloodGroup: string;
  referralCode?: string;
}

export interface RegisterDoctorDto {
  firstName: string; lastName: string; email: string;
  password: string; phoneNumber: string; specialty: string;
  qualification: string; experienceYears: number; consultationFee: number;
  bio: string; departmentId: number;
  medicalLicenseNumber: string; profileDocuments: string;
}

export interface ForgotPasswordDto {
  email: string;
}

export interface ResetPasswordDto {
  email: string;
  otp : string;
  newPassword: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  role: string;
  email: string;
  userId: number;
  referenceId?: number;
}
