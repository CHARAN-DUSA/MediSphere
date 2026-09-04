import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [MsIconComponent, ReactiveFormsModule, NgIf, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  showPassword = false;
  loading = false;
  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.auth.login(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Welcome back!');
        const role = this.auth.currentRole();
        if (role === 'Admin') this.router.navigate(['/admin', 'dashboard']);
        else if (role === 'Doctor') this.router.navigate(['/doctor', 'dashboard']);
        else this.router.navigate(['/patient']);
      },
      error: (err: HttpErrorResponse) => {
        this.loading = false;
        this.toast.error(this.extractErrorMessage(err));
      }
    });
  }

  private extractErrorMessage(err: HttpErrorResponse): string {
    // Network / server unreachable
    if (err.status === 0) {
      return 'Unable to reach the server. Check your internet connection.';
    }

    const body = err.error;

    // Common ASP.NET Core error shapes: string, { message }, { title }, { errors: {...} }
    if (typeof body === 'string' && body.trim()) {
      return body;
    }

    if (body?.message && typeof body.message === 'string') {
      return body.message;
    }

    if (body?.title && typeof body.title === 'string') {
      return body.title;
    }

    if (body?.errors) {
      const firstKey = Object.keys(body.errors)[0];
      const firstError = firstKey ? body.errors[firstKey]?.[0] : null;
      if (firstError) return firstError;
    }

    // Status-based fallback
    if (err.status === 401) return 'Invalid email or password.';
    if (err.status === 404) return 'Account not found.';
    if (err.status === 429) return 'Too many attempts. Please try again later.';
    if (err.status >= 500) return 'Server error. Please try again shortly.';

    return 'Login failed. Please check your credentials and try again.';
  }
}