import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { Appointment, CreateAppointmentDto } from '../models/appointment.model';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private baseUrl = `${environment.apiUrl}/appointments`;

  constructor(private http: HttpClient) {}

  getMyAppointments(page = 1, pageSize = 10) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<ApiResponse<PagedResult<Appointment>>>(`${this.baseUrl}/my`, { params });
  }

  getAppointmentById(id: number) {
    return this.http.get<ApiResponse<Appointment>>(`${this.baseUrl}/${id}`);
  }

  getAllAppointments(page = 1, pageSize = 10, doctorId?: number, status?: string) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (doctorId) params = params.set('doctorId', doctorId);
    if (status) params = params.set('status', status);
    return this.http.get<ApiResponse<PagedResult<Appointment>>>(this.baseUrl, { params });
  }

  createAppointment(dto: CreateAppointmentDto) {
    return this.http.post<ApiResponse<Appointment>>(this.baseUrl, dto);
  }

  cancelAppointment(id: number) {
    return this.http.delete<ApiResponse<void>>(`${this.baseUrl}/${id}`);
  }

  updateStatus(id: number, status: string, notes?: string) {
    return this.http.patch<ApiResponse<Appointment>>(`${this.baseUrl}/${id}/status`, { status, notes });
  }
}
