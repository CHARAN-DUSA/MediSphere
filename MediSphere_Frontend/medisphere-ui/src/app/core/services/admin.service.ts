import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { DashboardStats, SystemSetting, ContentItem } from '../models/admin.model';
import { Doctor } from '../models/doctor.model';
import { PatientProfile } from '../models/patient-profile.model';
import { BroadcastDto } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private baseUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getDashboard() {
    return this.http.get<ApiResponse<DashboardStats>>(`${this.baseUrl}/dashboard`);
  }

  approveDoctor(id: number, approve: boolean) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/doctors/${id}/approve`, { approve });
  }

  suspendDoctor(id: number) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/doctors/${id}/suspend`, {});
  }

  unblockDoctor(id: number) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/doctors/${id}/unblock`, {});
  }

  blockDoctor(id: number) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/doctors/${id}/block`, {});
  }

  getDoctors() {
    return this.http.get<ApiResponse<Doctor[]>>(`${this.baseUrl}/doctors`);
  }

  blockUser(email: string, block: boolean) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/users/block`, { email, block });
  }

  getPatients() {
    return this.http.get<ApiResponse<PatientProfile[]>>(`${this.baseUrl}/patients`);
  }

  getSettings() {
    return this.http.get<ApiResponse<SystemSetting[]>>(`${this.baseUrl}/settings`);
  }

  updateSetting(dto: SystemSetting) {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/settings`, dto);
  }

  getContent(type?: string) {
    let params = new HttpParams();
    if (type) params = params.set('type', type);
    return this.http.get<ApiResponse<ContentItem[]>>(`${this.baseUrl}/content`, { params });
  }

  upsertContent(dto: ContentItem) {
    return this.http.post<ApiResponse<ContentItem>>(`${this.baseUrl}/content`, dto);
  }

  deleteContent(id: number) {
    return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/content/${id}`);
  }

  broadcast(dto: BroadcastDto) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/broadcast`, dto);
  }
}
