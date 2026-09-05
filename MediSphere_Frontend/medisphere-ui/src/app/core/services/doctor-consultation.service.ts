import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { DoctorConsultation } from '../models/consultation.model';

@Injectable({
  providedIn: 'root'
})
export class DoctorConsultationService {
  private http = inject(HttpClient);

  private baseUrl =
    `${environment.apiUrl}/doctor-consultations`;

  getConsultation(appointmentId: number) {
    return this.http.get<ApiResponse<DoctorConsultation>>(
      `${this.baseUrl}/${appointmentId}`
    );
  }

  getMedicalRecordFile(recordId: number, appointmentId: number) {
    return this.http.get(
      `${environment.apiUrl}/medicalrecords/${recordId}/file?appointmentId=${appointmentId}`,
      {
        responseType: 'blob'
      }
    );
  }
}