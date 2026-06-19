export interface FamilyMember {
  id : number;
  name: string;
  relation: string; // Spouse, Child, Parent, etc.
  age: number;
}

export interface PatientProfile {
  id?: number;
  email?: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  address: string;
  bloodGroup: string;
  medicalHistory: string;
  isActive?: boolean;
  dateOfBirth?: string;
  gender?: string;
  createdAt?: string;
  familyMembers: FamilyMember[];
}
