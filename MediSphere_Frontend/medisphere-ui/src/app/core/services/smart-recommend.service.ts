import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Doctor } from '../models/doctor.model';

@Injectable({ providedIn: 'root' })
export class SmartRecommendService {
  private baseUrl = `${environment.apiUrl}/smartrecommend`;

  constructor(private http: HttpClient) {}

  getRecommendations(symptoms: string) {
    return this.http.get<ApiResponse<Doctor[]>>(`${this.baseUrl}?symptoms=${encodeURIComponent(symptoms)}`);
  }
}
