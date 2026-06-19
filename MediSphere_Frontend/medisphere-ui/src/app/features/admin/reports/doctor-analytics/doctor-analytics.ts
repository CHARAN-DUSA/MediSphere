import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../../core/services/admin.service';
import { AppointmentService } from '../../../../core/services/appointment.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Doctor } from '../../../../core/models/doctor.model';
import { Appointment } from '../../../../core/models/appointment.model';
import {
  AnalyticsPeriod,
  isDateInRange,
  periodLabel,
  resolvePeriodRange
} from '../analytics-period.util';

export interface DoctorLeaderboardRow {
  doctorId: number;
  name: string;
  specialty: string;
  departmentName: string;
  value: number;
  displayValue: string;
  rank: number;
}

export interface DepartmentRankRow {
  departmentName: string;
  appointmentCount: number;
  revenue: number;
  avgRating: number;
  cancellationRate: number;
  score: number;
  rank: number;
}

@Component({
  selector: 'app-doctor-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './doctor-analytics.html',
  styleUrls: ['./doctor-analytics.css']
})
export class DoctorAnalyticsComponent implements OnInit {

  private adminService = inject(AdminService);
  private appointmentService = inject(AppointmentService);
  private toast = inject(ToastService);

  loading = signal(false);
  extendedLoading = signal(false);
  extendedError = signal<string | null>(null);

  doctors = signal<Doctor[]>([]);
  appointments = signal<Appointment[]>([]);

  period = signal<AnalyticsPeriod>('month');
  customStart = signal('');
  customEnd = signal('');

  readonly periodOptions: AnalyticsPeriod[] = ['today', 'week', 'month', 'year', 'custom'];
  periodLabel = periodLabel;

  approvedDoctors = computed(() =>
    this.doctors().filter(d => d.isApproved).length
  );

  pendingDoctors = computed(() =>
    this.doctors().filter(d => !d.isApproved).length
  );

  activeDoctors = computed(() =>
    this.doctors().filter(d => d.isActive).length
  );

  inactiveDoctors = computed(() =>
    this.doctors().filter(d => !d.isActive).length
  );

  private periodRange = computed(() =>
    resolvePeriodRange(this.period(), this.customStart(), this.customEnd())
  );

  private filteredAppointments = computed(() => {
    const range = this.periodRange();
    return this.appointments().filter(a =>
      isDateInRange(a.appointmentDate, range)
    );
  });

  topEarningDoctors = computed(() =>
    this.buildEarningsLeaderboard(this.filteredAppointments())
  );

  mostAppointmentsDoctors = computed(() =>
    this.buildAppointmentLeaderboard(this.filteredAppointments())
  );

  highestRatedDoctors = computed(() =>
    this.buildRatingLeaderboard()
  );

  lowestCancellationDoctors = computed(() =>
    this.buildCancellationLeaderboard(this.filteredAppointments())
  );

  departmentRanking = computed(() =>
    this.buildDepartmentRanking(this.filteredAppointments())
  );

  ngOnInit(): void {
    this.loadDoctors();
    this.loadExtendedAnalytics();
  }

  loadDoctors(): void {
    this.loading.set(true);

    this.adminService.getDoctors()
      .subscribe({
        next: (response) => {
          this.doctors.set(response.data ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Failed to load doctor analytics.');
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
          this.extendedError.set('Failed to load extended doctor analytics.');
          this.toast.error('Failed to load extended doctor analytics.');
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

  getBarWidth(value: number, rows: { value: number }[]): number {
    const max = Math.max(...rows.map(r => r.value), 1);
    return (value / max) * 100;
  }

  getDeptScoreWidth(score: number, rows: DepartmentRankRow[]): number {
    const max = Math.max(...rows.map(r => r.score), 1);
    return (score / max) * 100;
  }

  private buildEarningsLeaderboard(appointments: Appointment[]): DoctorLeaderboardRow[] {
    const earnings = new Map<number, { total: number; name: string; specialty: string; department: string }>();

    for (const apt of appointments) {
      if (!this.isRevenueAppointment(apt)) continue;
      const current = earnings.get(apt.doctorId) ?? {
        total: 0,
        name: apt.doctorName,
        specialty: this.getDoctorSpecialty(apt.doctorId),
        department: apt.departmentName || 'Unassigned'
      };
      current.total += apt.fee ?? 0;
      earnings.set(apt.doctorId, current);
    }

    return [...earnings.entries()]
      .map(([doctorId, data]) => ({
        doctorId,
        name: data.name,
        specialty: data.specialty,
        departmentName: data.department,
        value: data.total,
        displayValue: `₹${data.total.toLocaleString('en-IN')}`,
        rank: 0
      }))
      .sort((a, b) => b.value - a.value)
      .slice(0, 10)
      .map((row, index) => ({ ...row, rank: index + 1 }));
  }

  private buildAppointmentLeaderboard(appointments: Appointment[]): DoctorLeaderboardRow[] {
    const counts = new Map<number, { count: number; name: string; specialty: string; department: string }>();

    for (const apt of appointments) {
      const current = counts.get(apt.doctorId) ?? {
        count: 0,
        name: apt.doctorName,
        specialty: this.getDoctorSpecialty(apt.doctorId),
        department: apt.departmentName || 'Unassigned'
      };
      current.count += 1;
      counts.set(apt.doctorId, current);
    }

    return [...counts.entries()]
      .map(([doctorId, data]) => ({
        doctorId,
        name: data.name,
        specialty: data.specialty,
        departmentName: data.department,
        value: data.count,
        displayValue: `${data.count}`,
        rank: 0
      }))
      .sort((a, b) => b.value - a.value)
      .slice(0, 10)
      .map((row, index) => ({ ...row, rank: index + 1 }));
  }

  private buildRatingLeaderboard(): DoctorLeaderboardRow[] {
    return [...this.doctors()]
      .filter(d => d.ratingCount > 0)
      .sort((a, b) => b.averageRating - a.averageRating || b.ratingCount - a.ratingCount)
      .slice(0, 10)
      .map((doctor, index) => ({
        doctorId: doctor.id,
        name: `Dr. ${doctor.firstName} ${doctor.lastName}`,
        specialty: doctor.specialty,
        departmentName: doctor.departmentName || 'Unassigned',
        value: doctor.averageRating,
        displayValue: `${doctor.averageRating.toFixed(1)} stars (${doctor.ratingCount})`,
        rank: index + 1
      }));
  }

  private buildCancellationLeaderboard(appointments: Appointment[]): DoctorLeaderboardRow[] {
    const stats = new Map<number, { total: number; cancelled: number; name: string; specialty: string; department: string }>();

    for (const apt of appointments) {
      const current = stats.get(apt.doctorId) ?? {
        total: 0,
        cancelled: 0,
        name: apt.doctorName,
        specialty: this.getDoctorSpecialty(apt.doctorId),
        department: apt.departmentName || 'Unassigned'
      };
      current.total += 1;
      if (apt.status === 'Cancelled') current.cancelled += 1;
      stats.set(apt.doctorId, current);
    }

    return [...stats.entries()]
      .filter(([, data]) => data.total >= 1)
      .map(([doctorId, data]) => {
        const rate = (data.cancelled / data.total) * 100;
        return {
          doctorId,
          name: data.name,
          specialty: data.specialty,
          departmentName: data.department,
          value: rate,
          displayValue: `${rate.toFixed(1)}%`,
          rank: 0
        };
      })
      .sort((a, b) => a.value - b.value)
      .slice(0, 10)
      .map((row, index) => ({ ...row, rank: index + 1 }));
  }

  private buildDepartmentRanking(appointments: Appointment[]): DepartmentRankRow[] {
    const departments = new Map<string, {
      appointmentCount: number;
      revenue: number;
      ratings: number[];
      cancelled: number;
    }>();

    for (const apt of appointments) {
      const dept = apt.departmentName || 'Unassigned';
      const current = departments.get(dept) ?? {
        appointmentCount: 0,
        revenue: 0,
        ratings: [],
        cancelled: 0
      };
      current.appointmentCount += 1;
      if (this.isRevenueAppointment(apt)) current.revenue += apt.fee ?? 0;
      if (apt.status === 'Cancelled') current.cancelled += 1;
      departments.set(dept, current);
    }

    for (const doctor of this.doctors()) {
      const dept = doctor.departmentName || 'Unassigned';
      const current = departments.get(dept) ?? {
        appointmentCount: 0,
        revenue: 0,
        ratings: [],
        cancelled: 0
      };
      if (doctor.ratingCount > 0) current.ratings.push(doctor.averageRating);
      departments.set(dept, current);
    }

    return [...departments.entries()]
      .map(([departmentName, data]) => {
        const avgRating = data.ratings.length
          ? data.ratings.reduce((sum, r) => sum + r, 0) / data.ratings.length
          : 0;
        const cancellationRate = data.appointmentCount
          ? (data.cancelled / data.appointmentCount) * 100
          : 0;
        const score = (data.appointmentCount * 2) + data.revenue + (avgRating * 10) - cancellationRate;
        return {
          departmentName,
          appointmentCount: data.appointmentCount,
          revenue: data.revenue,
          avgRating,
          cancellationRate,
          score,
          rank: 0
        };
      })
      .sort((a, b) => b.score - a.score)
      .map((row, index) => ({ ...row, rank: index + 1 }));
  }

  private getDoctorSpecialty(doctorId: number): string {
    return this.doctors().find(d => d.id === doctorId)?.specialty ?? 'General';
  }

  private isRevenueAppointment(apt: Appointment): boolean {
    return apt.paymentStatus === 'Paid'
      || apt.status === 'Completed'
      || apt.status === 'Confirmed';
  }
}
