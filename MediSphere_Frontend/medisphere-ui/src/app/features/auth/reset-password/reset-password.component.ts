import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [MsIconComponent, ReactiveFormsModule, NgIf, RouterLink],
  templateUrl: './reset-password.html',
  styleUrls: ['./reset-password.css']
})
export class ResetPasswordComponent implements OnInit
{
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);
  
  showPassword = false;
  loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    otp: [
      '',
      [
        Validators.required,
        Validators.pattern(/^\d{6}$/)
      ]
    ], newPassword: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])/)
    ]]
  });

  ngOnInit()
  {
    const emailParam = this.route.snapshot.queryParamMap.get('email');
    const tokenParam = this.route.snapshot.queryParamMap.get('token');
    if (emailParam)
    {
      this.form.patchValue({ email: emailParam });
    }
    if (tokenParam)
    {
      this.form.patchValue({ otp: tokenParam });
    }
  }

  onSubmit()
  {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.auth.resetPassword(this.form.value as any).subscribe({
      next: () =>
      {
        this.loading = false;
        this.toast.success('Password reset successfully! You can now log in.');
        this.router.navigate(['/login']);
      },
      error: () => { this.loading = false; }
    });
  }
  resendLoading = false;

  resendOtp() {
  const email = this.form.controls.email.value;

  if (!email) {
    this.toast.error('Enter email first');
    return;
  }

  this.resendLoading = true;

  this.auth.forgotPassword({email}).subscribe({
    next: () => {
      this.toast.success('OTP sent successfully');
      this.resendLoading = false;
    },
    error: () => {
      this.resendLoading = false;
    }
  });
}
}
