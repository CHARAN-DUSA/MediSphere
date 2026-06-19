export interface Appointment {
  id: number; patientId: number; patientName: string;
  doctorId: number; doctorName: string; departmentName: string;
  appointmentDate: string; startTime: string; endTime: string;
  status: string; reason: string; notes: string;
  isFollowUp: boolean; fee: number; createdAt: string;
  queueToken?: number;
  queueStatus?: string;
  paymentStatus?: string;
  meetingUrl?: string;
  meetingId?: string;
  razorpayOrderId?: string;
}
export interface CreateAppointmentDto {
  doctorId: number; appointmentDate: string;
  startTime: string; reason: string;
  isFollowUp: boolean; previousAppointmentId?: number;
}
