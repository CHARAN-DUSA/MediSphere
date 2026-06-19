import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminService } from '../../../../core/services/admin.service';
import { AppointmentService } from '../../../../core/services/appointment.service';
import { ToastService } from '../../../../core/services/toast.service';
import { PatientProfile } from '../../../../core/models/patient-profile.model';
import { Appointment } from '../../../../core/models/appointment.model';
import {
  AnalyticsPeriod,
  isDateInRange,
  periodLabel,
  resolvePeriodRange
} from '../analytics-period.util';

export interface DistributionSlice {
  label: string;
  count: number;
  color: string;
}

@Component({
  selector: 'app-patient-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './patient-analytics.html',
  styleUrls: ['./patient-analytics.css']
})
export class PatientAnalyticsComponent implements OnInit {

  private adminService = inject(AdminService);
  private appointmentService = inject(AppointmentService);
  private toast = inject(ToastService);

  loading = signal(false);
  extendedLoading = signal(false);
  extendedError = signal<string | null>(null);

  patients = signal<PatientProfile[]>([]);
  appointments = signal<Appointment[]>([]);

  period = signal<AnalyticsPeriod>('month');
  customStart = signal('');
  customEnd = signal('');

  readonly periodOptions: AnalyticsPeriod[] = ['today', 'week', 'month', 'year', 'custom'];
  periodLabel = periodLabel;

  activePatients = computed(() =>
    this.patients().filter(p => p.isActive !== false).length
  );

  blockedPatients = computed(() =>
    this.patients().filter(p => p.isActive === false).length
  );

  patientsWithHistory = computed(() =>
    this.patients().filter(
      p => p.medicalHistory && p.medicalHistory.trim().length > 0
    ).length
  );

  patientsWithFamilyMembers = computed(() =>
    this.patients().filter(
      p => p.familyMembers?.length > 0
    ).length
  );

  private periodRange = computed(() =>
    resolvePeriodRange(this.period(), this.customStart(), this.customEnd())
  );

  filteredPatientsByRegistration = computed(() => {
    const range = this.periodRange();
    return this.patients().filter(p =>
      isDateInRange(p.createdAt, range)
    );
  });

  totalPatientsInPeriod = computed(() => this.patients().length);

  newRegistrations = computed(() => this.filteredPatientsByRegistration().length);

  returningPatients = computed(() => {
    const range = this.periodRange();
    const appointmentsInPeriod = this.appointments().filter(a =>
      isDateInRange(a.appointmentDate, range)
    );
    const counts = new Map<number, number>();
    for (const apt of appointmentsInPeriod) {
      counts.set(apt.patientId, (counts.get(apt.patientId) ?? 0) + 1);
    }
    return [...counts.values()].filter(count => count >= 2).length;
  });

  activePatientsInPeriod = computed(() => {
    const range = this.periodRange();
    const activeIds = new Set(
      this.appointments()
        .filter(a => isDateInRange(a.appointmentDate, range))
        .map(a => a.patientId)
    );
    return activeIds.size;
  });

  ageDistribution = computed(() => this.buildAgeDistribution(this.patients()));

  genderDistribution = computed(() => this.buildGenderDistribution(this.patients()));

  ageDonutStyle = computed(() => this.buildDonutStyle(this.ageDistribution()));
  genderDonutStyle = computed(() => this.buildDonutStyle(this.genderDistribution()));

  hasAgeData = computed(() => this.ageDistribution().some(s => s.count > 0));
  hasGenderData = computed(() => this.genderDistribution().some(s => s.count > 0));

  ngOnInit(): void {
    this.loadPatients();
    this.loadExtendedAnalytics();
  }

  loadPatients(): void {
    this.loading.set(true);

    this.adminService.getPatients()
      .subscribe({
        next: (response) => {
          this.patients.set(response.data ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Failed to load patient analytics.');
        }
      });
  }

  loadExtendedAnalytics(): void {
    this.extendedLoading.set(true);
    this.extendedError.set(null);

    this.appointmentService.getAllAppointments(1, 500)
      .subscribe({
        next: (response) => {
          this.appointments.set(response.data?.items ?? []);
          this.extendedLoading.set(false);
        },
        error: () => {
          this.extendedLoading.set(false);
          this.extendedError.set('Failed to load extended patient analytics.');
          this.toast.error('Failed to load extended patient analytics.');
        }
      });
  }

  setPeriod(value: AnalyticsPeriod): void {
    this.period.set(value);
  }

  onCustomRangeChange(): void {
    if (this.period() === 'custom') {
      this.period.set('custom');
    }
  }

  reloadExtended(): void {
    this.loadExtendedAnalytics();
  }

  getDistributionBarWidth(count: number, slices: DistributionSlice[]): number {
    const max = Math.max(...slices.map(s => s.count), 1);
    return (count / max) * 100;
  }

  private buildAgeDistribution(patients: PatientProfile[]): DistributionSlice[] {
    const buckets = [
      { label: '0-17', min: 0, max: 17, count: 0, color: '#60a5fa' },
      { label: '18-30', min: 18, max: 30, count: 0, color: '#34d399' },
      { label: '31-45', min: 31, max: 45, count: 0, color: '#fbbf24' },
      { label: '46-60', min: 46, max: 60, count: 0, color: '#f97316' },
      { label: '60+', min: 61, max: 200, count: 0, color: '#a78bfa' }
    ];

    for (const patient of patients) {
      const age = this.calculateAge(patient.dateOfBirth);
      if (age == null) continue;
      const bucket = buckets.find(b => age >= b.min && age <= b.max);
      if (bucket) bucket.count += 1;
    }

    return buckets.map(({ label, count, color }) => ({ label, count, color }));
  }

  private buildGenderDistribution(patients: PatientProfile[]): DistributionSlice[] {
    const palette = ['#3b82f6', '#ec4899', '#8b5cf6', '#64748b'];
    const counts = new Map<string, number>();

    for (const patient of patients) {
      const gender = (patient.gender || 'Unknown').trim() || 'Unknown';
      const key = gender.charAt(0).toUpperCase() + gender.slice(1).toLowerCase();
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }

    return [...counts.entries()].map(([label, count], index) => ({
      label,
      count,
      color: palette[index % palette.length]
    }));
  }

  private buildDonutStyle(slices: DistributionSlice[]): string {
    const total = slices.reduce((sum, s) => sum + s.count, 0) || 1;
    let cursor = 0;
    const segments = slices.map(slice => {
      const pct = (slice.count / total) * 100;
      const start = cursor;
      cursor += pct;
      return `${slice.color} ${start}% ${cursor}%`;
    });
    return `conic-gradient(${segments.join(', ')})`;
  }

  private calculateAge(dateOfBirth?: string): number | null {
    if (!dateOfBirth) return null;
    const dob = new Date(dateOfBirth);
    if (Number.isNaN(dob.getTime())) return null;
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
      age -= 1;
    }
    return age;
  }
}
