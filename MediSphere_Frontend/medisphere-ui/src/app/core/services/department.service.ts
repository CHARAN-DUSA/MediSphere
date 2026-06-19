import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
export interface Department {
  id: number; name: string; description: string;
  iconUrl: string; doctorCount: number;
}

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private baseUrl = `${environment.apiUrl}/departments`;
  constructor(private http: HttpClient) {}
  getAll() { return this.http.get<ApiResponse<Department[]>>(this.baseUrl); }
  create(dto: any) { return this.http.post<ApiResponse<Department>>(this.baseUrl, dto); }
  update(id: number, dto: any) { return this.http.put<ApiResponse<Department>>(`${this.baseUrl}/${id}`, dto); }
  delete(id: number) { return this.http.delete<ApiResponse<object>>(`${this.baseUrl}/${id}`); }
}
