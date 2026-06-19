import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { FamilyMember, PatientProfile } from '../models/patient-profile.model';
import { Doctor } from '../models/doctor.model';
import { Notification } from '../models/notification.model';
import { MedicalRecord } from '../models/medical-record.model';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private baseUrl = `${environment.apiUrl}/patients`;
  private recordsUrl = `${environment.apiUrl}/medicalrecords`;

  constructor(private http: HttpClient) {}

  getProfile() {
    return this.http.get<ApiResponse<PatientProfile>>(`${this.baseUrl}/profile`);
  }

  updateProfile(profile: PatientProfile) {
    return this.http.put<ApiResponse<PatientProfile>>(`${this.baseUrl}/profile`, profile);
  }

  getFavorites() {
    return this.http.get<ApiResponse<Doctor[]>>(`${this.baseUrl}/favorites`);
  }

  toggleFavorite(doctorId: number) {
    return this.http.post<ApiResponse<boolean>>(`${this.baseUrl}/favorites/${doctorId}`, {});
  }

  getNotifications() {
    return this.http.get<ApiResponse<Notification[]>>(`${this.baseUrl}/notifications`);
  }

  markNotificationAsRead(id: number) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/notifications/${id}/read`, {});
  }

  markAllNotificationsAsRead() {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/notifications/read-all`, {});
  }

  getMedicalRecords(patientId: number) {
    return this.http.get<ApiResponse<MedicalRecord[]>>(`${this.recordsUrl}/patient/${patientId}`);
  }

  uploadMedicalRecord(file: File, description: string, appointmentId?: number) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('description', description);
    if (appointmentId) {
      formData.append('appointmentId', appointmentId.toString());
    }
    return this.http.post<ApiResponse<MedicalRecord>>(`${this.recordsUrl}/upload`, formData);
  }

  deleteMedicalRecord(id: number) {
    return this.http.delete<ApiResponse<object>>(`${this.recordsUrl}/${id}`);
  }

  addFamilyMember(member: FamilyMember) {
  return this.http.post<any>(
    `${environment.apiUrl}/patients/family-members`,
    member
  );
}

deleteFamilyMember(id: number) {
  return this.http.delete(
    `${environment.apiUrl}/patients/family-members/${id}`
  );
}
}
