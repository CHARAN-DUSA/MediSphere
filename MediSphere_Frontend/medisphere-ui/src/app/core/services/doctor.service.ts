import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { Doctor, DoctorFilter, DoctorSchedule, BlockSlotDto, VacationDto, DoctorEarningsDto } from '../models/doctor.model';
import { Notification } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class DoctorService {
  private baseUrl = `${environment.apiUrl}/doctors`;

  constructor(private http: HttpClient) {}

  getDoctors(filter: DoctorFilter = {}) {
    let params = new HttpParams()
      .set('page', filter.page ?? 1)
      .set('pageSize', filter.pageSize ?? 10);
      
    if (filter.specialty) params = params.set('specialty', filter.specialty);
    if (filter.departmentId) params = params.set('departmentId', filter.departmentId);
    if (filter.search) params = params.set('search', filter.search);
    if (filter.gender) params = params.set('gender', filter.gender);
    if (filter.location) params = params.set('location', filter.location);
    if (filter.language) params = params.set('language', filter.language);
    if (filter.minFee !== undefined) params = params.set('minFee', filter.minFee);
    if (filter.maxFee !== undefined) params = params.set('maxFee', filter.maxFee);
    if (filter.minRating !== undefined) params = params.set('minRating', filter.minRating);
    if (filter.isAvailable !== undefined) params = params.set('isAvailable', filter.isAvailable);

    return this.http.get<ApiResponse<PagedResult<Doctor>>>(this.baseUrl, { params });
  }

  getDoctorById(id: number) {
    return this.http.get<ApiResponse<Doctor>>(`${this.baseUrl}/${id}`);
  }

  getAvailableSlots(doctorId: number, date: string) {
    return this.http.get<ApiResponse<string[]>>(`${environment.apiUrl}/appointments/slots`, {
      params: new HttpParams().set('doctorId', doctorId).set('date', date)
    });
  }

  updateSchedule(doctorId: number, schedules: DoctorSchedule[]) {
    return this.http.put<ApiResponse<object>>(`${this.baseUrl}/${doctorId}/schedule`, schedules);
  }

  blockSlot(doctorId: number, blockDto: BlockSlotDto) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/${doctorId}/block-slot`, blockDto);
  }

  setVacation(doctorId: number, vacationDto: VacationDto) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/${doctorId}/vacation`, vacationDto);
  }

  uploadProfileImage(doctorId: number, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<string>>(`${this.baseUrl}/${doctorId}/profile-image`, formData);
  }

  updateDoctor(id: number, dto: any) {
    return this.http.put<ApiResponse<Doctor>>(`${this.baseUrl}/${id}`, dto);
  }

  getDoctorEarnings(doctorId: number) {
    return this.http.get<ApiResponse<DoctorEarningsDto>>(`${this.baseUrl}/${doctorId}/earnings`);
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
}

