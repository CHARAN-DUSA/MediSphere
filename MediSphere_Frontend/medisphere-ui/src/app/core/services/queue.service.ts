import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Appointment } from '../models/appointment.model';

@Injectable({ providedIn: 'root' })
export class QueueService {
  private baseUrl = `${environment.apiUrl}/queue`;

  constructor(private http: HttpClient) {}

  getDoctorQueue(doctorId: number) {
    return this.http.get<ApiResponse<Appointment[]>>(`${this.baseUrl}/doctor/${doctorId}`);
  }

  callNextPatient(doctorId: number) {
    return this.http.post<ApiResponse<Appointment>>(`${this.baseUrl}/call-next/${doctorId}`, {});
  }

  updateQueueStatus(appointmentId: number, queueStatus: string) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/update-status/${appointmentId}`, { queueStatus });
  }
}
