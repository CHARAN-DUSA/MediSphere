export interface Review {
  id: number;
  patientId: number;
  patientName: string;
  doctorId: number;
  doctorName: string;
  appointmentId: number;
  rating: number;
  comment: string;
  status: string;
  createdAt: string;
  doctorResponse?: string;
  responseCreatedAt?: string;
}

export interface CreateReviewDto {
  doctorId: number;
  appointmentId: number;
  rating: number;
  comment: string;
}
