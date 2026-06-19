import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { MsIconComponent } from '../../shared/components/ms-icon/ms-icon.component';
import { DepartmentService, Department } from '../../core/services/department.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    MsIconComponent,
    RouterLink,
    NgFor,
    NgIf,
    FormsModule
  ],
  templateUrl: './home.html',
  styleUrls: ['./home.css']
})
export class HomeComponent implements OnInit {

  private deptService = inject(DepartmentService);
  private router = inject(Router);

  departments = signal<Department[]>([]);

  searchQuery = '';
  selectedDepartmentId: number | '' = '';

  steps = [
    {
      num: '1',
      title: 'Register & Login',
      desc: 'Create your account in minutes and verify your identity.'
    },
    {
      num: '2',
      title: 'Find Your Doctor',
      desc: 'Search doctors by specialty, department, or expertise.'
    },
    {
      num: '3',
      title: 'Book & Consult',
      desc: 'Choose a slot and book your appointment instantly.'
    }
  ];

  ngOnInit(): void {
    this.loadDepartments();
  }

  private loadDepartments(): void {
    this.deptService.getAll().subscribe({
      next: (response) => {
        this.departments.set(response.data);
      },
      error: (err) => {
        console.error('Failed to load departments', err);
      }
    });
  }

search(): void {
  const queryParams: Record<string, any> = {};

  const search = this.searchQuery.trim();

  if (search.length > 0) {
    queryParams['search'] = search;
  }

  if (this.selectedDepartmentId !== '') {
    queryParams['departmentId'] = this.selectedDepartmentId;
  }

  console.log('Navigating with:', queryParams);

  this.router.navigate(['/doctors'], {
    queryParams
  });
}

  getDepartmentIcon(name: string): string {
    const icons: Record<string, string> = {
  Cardiology: 'favorite',
  Neurology: 'psychology',
  Orthopedics: 'accessibility_new',
  Pediatrics: 'child_care',
  Dermatology: 'water_drop',

  Nephrology: 'water',
  Endocrinology: 'science',
  Gastroenterology: 'restaurant',
  Pulmonology: 'air',
  Oncology: 'biotech',
  Rheumatology: 'healing',
  Urology: 'medical_services',
  'Sports Medicine': 'sports_soccer',
  'Sleep Medicine': 'bedtime',
  Geriatrics: 'elderly',

  Gynecology: 'female',
  Psychiatry: 'psychology_alt',
  'General Medicine': 'local_hospital',
  General: 'local_hospital'
};

    return icons[name] ?? 'medical_services';
  }

  getDepartmentColor(name: string): string {
    const colors: Record<string, string> = {
  Cardiology: '#dc2626',
  Neurology: '#7c3aed',
  Orthopedics: '#2563eb',
  Dermatology: '#be185d',
  Pediatrics: '#16a34a',

  Nephrology: '#0891b2',
  Endocrinology: '#f59e0b',
  Gastroenterology: '#ea580c',
  Pulmonology: '#06b6d4',
  Oncology: '#9333ea',
  Rheumatology: '#8b5cf6',
  Urology: '#0f766e',
  'Sports Medicine': '#22c55e',
  'Sleep Medicine': '#6366f1',
  Geriatrics: '#64748b',

  Gynecology: '#ea580c',
  Psychiatry: '#0284c7',
  'General Medicine': '#1d4ed8',
  General: '#1d4ed8'
};

    return colors[name] ?? '#0a3d62';
  }

  getDepartmentBg(name: string): string {
    const backgrounds: Record<string, string> = {
  Cardiology: '#fee2e2',
  Neurology: '#ede9fe',
  Orthopedics: '#dbeafe',
  Dermatology: '#fce7f3',
  Pediatrics: '#dcfce7',

  Nephrology: '#cffafe',
  Endocrinology: '#fef3c7',
  Gastroenterology: '#ffedd5',
  Pulmonology: '#cffafe',
  Oncology: '#f3e8ff',
  Rheumatology: '#ede9fe',
  Urology: '#ccfbf1',
  'Sports Medicine': '#dcfce7',
  'Sleep Medicine': '#e0e7ff',
  Geriatrics: '#f1f5f9',

  Gynecology: '#ffedd5',
  Psychiatry: '#e0f2fe',
  'General Medicine': '#eff6ff',
  General: '#eff6ff'
};

    return backgrounds[name] ?? '#f8fafc';
  }
}