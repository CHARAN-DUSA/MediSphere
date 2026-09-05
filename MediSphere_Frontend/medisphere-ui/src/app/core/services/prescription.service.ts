import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import {
  Prescription,
  CreatePrescription
} from '../models/prescription.model';

@Injectable({
  providedIn: 'root'
})
export class PrescriptionService {
  private http = inject(HttpClient);

  private baseUrl =
    `${environment.apiUrl}/prescriptions`;

  create(dto: CreatePrescription) {
    return this.http.post<ApiResponse<Prescription>>(
      this.baseUrl,
      dto
    );
  }

  getPatientHistory(patientId: number) {
    return this.http.get<ApiResponse<Prescription[]>>(
      `${this.baseUrl}/patient/${patientId}`
    );
  }

  getByAppointment(appointmentId: number) {
    return this.http.get<ApiResponse<Prescription>>(
      `${this.baseUrl}/appointment/${appointmentId}`
    );
  }
}