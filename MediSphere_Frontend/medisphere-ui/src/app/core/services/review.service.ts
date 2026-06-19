import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { Review, CreateReviewDto } from '../models/review.model';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private baseUrl = `${environment.apiUrl}/reviews`;

  constructor(private http: HttpClient) {}

  createReview(dto: CreateReviewDto) {
    return this.http.post<ApiResponse<Review>>(this.baseUrl, dto);
  }

  getDoctorReviews(doctorId: number) {
    return this.http.get<ApiResponse<Review[]>>(`${this.baseUrl}/doctor/${doctorId}`);
  }

  getPendingReviews() {
    return this.http.get<ApiResponse<Review[]>>(`${this.baseUrl}/pending`);
  }

  moderateReview(id: number, status: string) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/${id}/moderate`, { status });
  }

  respondToReview(id: number, responseText: string) {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/${id}/respond`, { responseText });
  }
}
