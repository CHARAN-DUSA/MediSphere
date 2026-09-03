export interface MedicalRecord {
  id: number;
  patientId: number;
  appointmentId?: number;
  fileUrl: string;
  fileName: string;
  fileType?: string;
  fileSizeBytes?: number;
  description: string;
  uploadedAt?: string;
  createdAt?: string;
}
