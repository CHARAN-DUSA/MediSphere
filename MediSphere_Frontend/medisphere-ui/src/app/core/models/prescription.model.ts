export interface PrescriptionMedicine {
  id?: number;
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  route: string;
  instructions: string;
}

export interface Prescription {
  id: number;
  patientId: number;
  patientName: string;
  doctorId: number;
  doctorName: string;
  appointmentId: number;
  appointmentDate: string;
  diagnosis: string;
  clinicalNotes: string;
  instructions: string;
  followUpDate?: string | null;
  createdAt: string;
  medicines: PrescriptionMedicine[];
}

export interface CreatePrescriptionMedicine {
  medicineName: string;
  dosage: string;
  frequency: string;
  duration: string;
  route: string;
  instructions: string;
}

export interface CreatePrescription {
  appointmentId: number;
  diagnosis: string;
  clinicalNotes: string;
  instructions: string;
  followUpDate?: string | null;
  medicines: CreatePrescriptionMedicine[];
}