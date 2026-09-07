import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import {DepartmentService, Department } from '../../../core/services/department.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [MsIconComponent, ReactiveFormsModule, NgFor, NgIf, RouterLink],
  templateUrl:'./register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private departmentService = inject(DepartmentService);
  
  showPassword = false
  loading = false;
  selectedRole: 'Patient' | 'Doctor' = 'Patient';
  bloodGroups = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'];
  
  departments: Department[] = [];

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])/)]],
    phoneNumber: ['', Validators.required],
    gender: ['', Validators.required],
    dateOfBirth: [''],
    address: [''],
    bloodGroup: [''],
    referralCode: [''],
    specialty: [''],
    qualification: [''],
    experienceYears: [0],
    consultationFee: [0],
    bio: [''],
    departmentId: [null],
    medicalLicenseNumber: [''],
    profileDocuments: ['']
  });

  ngOnInit() {
  this.setRole('Patient');
  this.loadDepartments();
}

loadDepartments(): void {
  this.departmentService.getAll().subscribe({
    next: (response) => {
      this.departments = response.data ?? [];
    },
    error: () => {
      this.toast.error('Failed to load departments.');
    }
  });
}

  setRole(role: 'Patient' | 'Doctor') {
    this.selectedRole = role;
    if (role === 'Patient') {
      this.form.get('dateOfBirth')?.setValidators(Validators.required);
      this.form.get('address')?.setValidators(Validators.required);
      this.form.get('bloodGroup')?.setValidators(Validators.required);
      this.form.get('specialty')?.clearValidators();
      this.form.get('qualification')?.clearValidators();
      this.form.get('medicalLicenseNumber')?.clearValidators();
    } else {
      this.form.get('dateOfBirth')?.clearValidators();
      this.form.get('address')?.clearValidators();
      this.form.get('bloodGroup')?.clearValidators();
      this.form.get('specialty')?.setValidators(Validators.required);
      this.form.get('qualification')?.setValidators(Validators.required);
      this.form.get('medicalLicenseNumber')?.setValidators(Validators.required);
    }
    
    this.form.get('dateOfBirth')?.updateValueAndValidity();
    this.form.get('address')?.updateValueAndValidity();
    this.form.get('bloodGroup')?.updateValueAndValidity();
    this.form.get('specialty')?.updateValueAndValidity();
    this.form.get('qualification')?.updateValueAndValidity();
    this.form.get('medicalLicenseNumber')?.updateValueAndValidity();
    this.form.get('departmentId')?.updateValueAndValidity();
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    
    this.loading = true;
    
    if (this.selectedRole === 'Patient') {
      this.auth.register(this.form.value as any).subscribe({
        next: () => {
          this.toast.success('Patient registration successful!');
          this.router.navigate(['/patient']);
        },
        error: () => { this.loading = false; }
      });
    } else {
      this.auth.registerDoctor(this.form.value as any).subscribe({
        next: () => {
          this.toast.success('Doctor application submitted successfully! Pending approval.');
          this.router.navigate(['/login']);
        },
        error: () => { this.loading = false; }
      });
    }
  }
}
