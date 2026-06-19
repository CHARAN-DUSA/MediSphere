import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginDto, RegisterPatientDto, RegisterDoctorDto, ForgotPasswordDto, ResetPasswordDto } from '../models/auth.model';
import { ApiResponse } from '../models/api-response.model';

@Injectable({ providedIn: 'root' })
export class AuthService
{
  [x: string]: any;
  private readonly baseUrl = `${environment.apiUrl}/auth`;
  isLoggedIn = signal(!!localStorage.getItem('token'));
  currentRole = signal(localStorage.getItem('role') ?? '');
  currentUserId = signal(+(localStorage.getItem('userId') ?? '0'));
  referenceId = signal(+(localStorage.getItem('referenceId') ?? '0'));


  constructor(private http: HttpClient, private router: Router) { }

  login(dto: LoginDto)
  {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/login`, dto).pipe(
      tap(r => this.storeTokens(r.data))
    );
  }

  register(dto: RegisterPatientDto)
  {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/register`, dto).pipe(
      tap(r => this.storeTokens(r.data))
    );
  }

  registerDoctor(dto: RegisterDoctorDto)
  {
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/register/doctor`, dto);
  }

  forgotPassword(dto: ForgotPasswordDto)
  {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/forgot-password`, dto);
  }

  resetPassword(dto: ResetPasswordDto)
  {
    return this.http.post<ApiResponse<object>>(`${this.baseUrl}/reset-password`, dto);
  }

  logout()
  {
    this.http.post(`${this.baseUrl}/revoke`, {}).subscribe();
    localStorage.clear();
    this.isLoggedIn.set(false);
    this.currentRole.set('');
    this.currentUserId.set(0);
    this.referenceId.set(0);
    this.router.navigate(['/login']);
  }

  refreshToken()
  {
    const token = localStorage.getItem('token');
    const refresh = localStorage.getItem('refreshToken');
    return this.http.post<ApiResponse<AuthResponse>>(`${this.baseUrl}/refresh`, {
      accessToken: token, refreshToken: refresh
    }).pipe(tap(r => this.storeTokens(r.data)));
  }

  private storeTokens(data: AuthResponse)
  {
    localStorage.setItem('token', data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    localStorage.setItem('role', data.role);
    localStorage.setItem('userId', data.userId.toString());
    localStorage.setItem('referenceId', (data.referenceId || 0).toString());

    this.isLoggedIn.set(true);
    this.currentRole.set(data.role);
    this.currentUserId.set(data.userId);
    this.referenceId.set(data.referenceId || 0);
  }

  getToken() { return localStorage.getItem('token'); }
  getRole() { return localStorage.getItem('role'); }
  getReferenceId() { return localStorage.getItem('referenceId'); }
}
