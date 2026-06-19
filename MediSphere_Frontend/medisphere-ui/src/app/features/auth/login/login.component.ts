import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [MsIconComponent, ReactiveFormsModule, NgIf, RouterLink],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent
{
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  showPassword = false
  loading = false;
  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });
  onSubmit()
  {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.auth.login(this.form.value as any).subscribe({
      next: () =>
      {
        this.toast.success('Welcome back!');
        const role = this.auth.currentRole();
        if (role === 'Admin') this.router.navigate(['/admin', 'dashboard']);
        else if (role === 'Doctor') this.router.navigate(['/doctor', 'dashboard']);
        else this.router.navigate(['/patient']);
      },
      error: (err) => {

  this.loading = false;

  const message = JSON.stringify(err.error).toLowerCase();

  if (message.includes('invalid email')) {
    this.toast.error('Invalid email address.');
  }
  else if (message.includes('invalid password')) {
    this.toast.error('Invalid password.');
  }
}
    });
  }
}
