import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Doctor } from '../models/doctor.model';

export interface RewardStatement {
  currentPoints: number;
  referralCode: string;
  transactionHistory: RewardLog[];
}

export interface RewardLog {
  id: number;
  patientId: number;
  points: number;
  action: string;
  description: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class RewardsService {
  private baseUrl = `${environment.apiUrl}/rewards`;

  constructor(private http: HttpClient) {}

  getMyStatement() {
    return this.http.get<ApiResponse<RewardStatement>>(`${this.baseUrl}/my-statement`);
  }

  getRules() {
    return this.http.get<ApiResponse<Record<string, number>>>(`${this.baseUrl}/rules`);
  }
}
