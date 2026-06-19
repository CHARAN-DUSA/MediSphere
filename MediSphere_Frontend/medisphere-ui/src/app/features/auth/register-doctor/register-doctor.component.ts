import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgIf, NgFor } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { DepartmentService, Department } from '../../../core/services/department.service';

@Component({
  selector: 'app-register-doctor',
  standalone: true,
  imports: [MsIconComponent, ReactiveFormsModule, NgIf, NgFor, RouterLink],
  templateUrl: './register-doctor.html',
  styleUrls: ['./register-doctor.css']
})
export class RegisterDoctorComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private deptService = inject(DepartmentService);
  private router = inject(Router);
  private toast = inject(ToastService);
  
  showPassword= false
  loading = false;
  departments = signal<Department[]>([]);

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    phoneNumber: ['', Validators.required],
    departmentId: ['', Validators.required],
    specialty: ['', Validators.required],
    qualification: ['', Validators.required],
    experienceYears: [null as any, [Validators.required, Validators.min(0)]],
    consultationFee: [null as any, [Validators.required, Validators.min(0)]],
    bio: ['', Validators.required]
  });

  ngOnInit() {
    this.deptService.getAll().subscribe(r => this.departments.set(r.data));
  }

  onSubmit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    
    // Prepare DTO
    const val = this.form.value;
    const dto = {
      ...val,
      departmentId: +val.departmentId!,
      experienceYears: +val.experienceYears!,
      consultationFee: +val.consultationFee!
    };

    this.auth.registerDoctor(dto as any).subscribe({
      next: () => {
        this.loading = false;
        this.toast.success('Registration successful. Admin will review and approve your profile.');
        this.router.navigate(['/login']);
      },
      error: () => { this.loading = false; }
    });
  }
}
