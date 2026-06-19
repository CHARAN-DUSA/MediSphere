import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [MsIconComponent, ReactiveFormsModule, NgIf, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrls: ['./forgot-password.css']
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);
  
  loading = false;
  sent = signal(false);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.auth.forgotPassword(this.form.value as any).subscribe({
      next: () => {
        this.loading = false;
        this.sent.set(true);
        this.toast.success('Reset instructions sent to your email.');
        // After 3 seconds, redirect to reset password page automatically
        setTimeout(() => {
          this.router.navigate(['/reset-password'], { queryParams: { email: this.form.value.email } });
        }, 4000);
      },
      error: () => { this.loading = false; }
    });
  }
}
