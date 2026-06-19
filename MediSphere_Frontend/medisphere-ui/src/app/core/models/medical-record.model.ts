export interface MedicalRecord {
  id: number;
  patientId: number;
  appointmentId?: number;
  fileUrl: string;
  fileName: string;
  description: string;
  createdAt: string;
}
