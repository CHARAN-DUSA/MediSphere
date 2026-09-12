import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { DashboardStats, SystemSetting, ContentItem } from '../models/admin.model';
import { Doctor } from '../models/doctor.model';
import { PatientProfile } from '../models/patient-profile.model';
import { BroadcastDto } from '../models/notification.model';

// =========================================================
// Admin Patient Interfaces
// =========================================================
export interface AdminPatientPayment {
  paymentTransactionId: number;
  appointmentId: number;
  doctorName: string;
  grossAmount: number;
  refundAmount: number;
  status: string;
  refundStatus: string;
  razorpayPaymentId?: string;
  razorpayRefundId?: string;
  createdAt: string;
}

export interface AdminPatientDetail extends PatientProfile {
  totalAppointments?: number;
  totalSpend?: number;
  totalRefunded?: number;
  isDeleted?: boolean;
  createdAt?: string;
  recentAppointments?: any[];
  paymentHistory?: AdminPatientPayment[];
}

// =========================================================
// Admin Doctor Interfaces
// =========================================================
export interface AdminDoctorBlockedSlot {
  id: number;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  reason: string;
}

export interface AdminDoctorTransaction {
  paymentTransactionId: number;
  appointmentId: number;
  patientName: string;
  grossAmount: number;
  netDoctorAmount: number;
  adminCommission: number;
  refundAmount: number;
  status: string;
  refundStatus: string;
  createdAt: string;
}

export interface AdminDoctorDetail extends Doctor {
  totalAppointments?: number;
  completedAppointments?: number;
  cancelledAppointments?: number;
  totalGrossEarnings?: number;
  totalNetEarnings?: number;
  totalRefunds?: number;
  totalAdminCommission?: number;
  isDeleted?: boolean;
  createdAt?: string;
  recentAppointments?: any[];
  blockedSlots?: AdminDoctorBlockedSlot[];
  transactions?: AdminDoctorTransaction[];
  refundedAppointmentsCount?: number;
}

// =========================================================
// Admin Refund Interfaces
// =========================================================
export interface AdminRefund {
  paymentTransactionId: number;
  appointmentId: number;
  patientName: string;
  patientEmail: string;
  doctorName: string;
  doctorSpecialty: string;
  originalAmount: number;
  refundAmount: number;
  paymentStatus: string;
  refundStatus: string; // NotApplicable|RefundRequested|RefundProcessing|Refunded|RefundFailed|Simulated|PartiallyRefunded
  razorpayPaymentId?: string;
  razorpayRefundId?: string;
  cancellationReason?: string;
  cancelledBy?: string;
  cancelledAt?: string;
  refundInitiatedAt?: string;
  refundCompletedAt?: string;
  refundFailureReason?: string;
  refundDestination: string;
  createdAt: string;
}

export interface AdminRefundDetail extends AdminRefund {
  doctorDepartment?: string;
  patientPhone?: string;
  appointmentDate?: string;
  startTime?: string;
  adminCommission?: number;
  doctorEarnings?: number;
  netDoctorAmount?: number;
}

export interface AdminRefundSummary {
  totalRefunds: number;
  totalRefundedAmount: number;
  pendingCount: number;
  processingCount: number;
  completedCount: number;
  failedCount: number;
}

// =========================================================
// Service
// =========================================================
@Injectable({ providedIn: 'root' })
export class AdminService {
  private baseUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  getDashboard() {
    return this.http.get<ApiResponse<DashboardStats>>(`${this.baseUrl}/dashboard`);
  }

  // ----- Doctor Management -----
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

  searchDoctors(query: string = '', page = 1, pageSize = 10) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (query) params = params.set('query', query);
    return this.http.get<ApiResponse<PagedResult<Doctor>>>(`${this.baseUrl}/doctors/search`, { params });
  }

  getDoctorDetail(id: number) {
    return this.http.get<ApiResponse<AdminDoctorDetail>>(`${this.baseUrl}/doctors/${id}`);
  }

  deleteDoctor(id: number, reason: string = 'Admin initiated deletion') {
    let params = new HttpParams().set('reason', reason);
    return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/doctors/${id}`, { params });
  }

  // ----- Patient Management -----
  blockUser(email: string, block: boolean) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/users/block`, { email, block });
  }

  getPatients() {
    return this.http.get<ApiResponse<PatientProfile[]>>(`${this.baseUrl}/patients`);
  }

  searchPatients(query: string = '', page = 1, pageSize = 10) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (query) params = params.set('query', query);
    return this.http.get<ApiResponse<PagedResult<PatientProfile>>>(`${this.baseUrl}/patients/search`, { params });
  }

  getPatientDetail(id: number) {
    return this.http.get<ApiResponse<AdminPatientDetail>>(`${this.baseUrl}/patients/${id}`);
  }

  deletePatient(id: number, reason: string = 'Admin initiated deletion') {
    let params = new HttpParams().set('reason', reason);
    return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/patients/${id}`, { params });
  }

  // ----- Refund Management -----
  getRefundSummary() {
    return this.http.get<ApiResponse<AdminRefundSummary>>(`${this.baseUrl}/refunds/summary`);
  }

  getRefunds(query = '', status = '', fromDate = '', toDate = '', page = 1, pageSize = 10) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (query) params = params.set('query', query);
    if (status) params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);
    return this.http.get<ApiResponse<PagedResult<AdminRefund>>>(`${this.baseUrl}/refunds`, { params });
  }

  getRefundDetail(id: number) {
    return this.http.get<ApiResponse<AdminRefundDetail>>(`${this.baseUrl}/refunds/${id}`);
  }

  retryRefund(id: number) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/refunds/${id}/retry`, {});
  }

  // ----- Settings & Content -----
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
