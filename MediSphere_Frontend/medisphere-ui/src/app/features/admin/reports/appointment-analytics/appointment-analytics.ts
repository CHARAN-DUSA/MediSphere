import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../../core/services/admin.service';
import { AppointmentService } from '../../../../core/services/appointment.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Appointment } from '../../../../core/models/appointment.model';
import {
  AnalyticsPeriod,
  isDateInRange,
  periodLabel,
  resolvePeriodRange
} from '../analytics-period.util';

export interface StatusMetric {
  label: string;
  count: number;
  color: string;
}

export interface DepartmentTrendRow {
  departmentName: string;
  booked: number;
  completed: number;
  cancelled: number;
  noShow: number;
  revenue: number;
}

@Component({
  selector: 'app-appointment-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './appointment-analytics.html',
  styleUrls: ['./appointment-analytics.css']
})
export class AppointmentAnalyticsComponent implements OnInit {

  private adminService = inject(AdminService);
  private appointmentService = inject(AppointmentService);
  private toast = inject(ToastService);

  loading = signal(false);
  extendedLoading = signal(false);
  extendedError = signal<string | null>(null);

  dashboardData = signal<any>(null);
  appointments = signal<Appointment[]>([]);

  period = signal<AnalyticsPeriod>('month');
  customStart = signal('');
  customEnd = signal('');

  readonly periodOptions: AnalyticsPeriod[] = ['today', 'week', 'month', 'year', 'custom'];
  periodLabel = periodLabel;

  private periodRange = computed(() =>
    resolvePeriodRange(this.period(), this.customStart(), this.customEnd())
  );

  filteredAppointments = computed(() => {
    const range = this.periodRange();
    return this.appointments().filter(a =>
      isDateInRange(a.appointmentDate, range)
    );
  });

  bookedCount = computed(() =>
    this.filteredAppointments().filter(a =>
      ['Pending', 'Confirmed', 'PendingPayment', 'Rescheduled'].includes(a.status)
    ).length
  );

  completedCount = computed(() =>
    this.filteredAppointments().filter(a => a.status === 'Completed').length
  );

  cancelledCount = computed(() =>
    this.filteredAppointments().filter(a => a.status === 'Cancelled').length
  );

  noShowCount = computed(() =>
    this.filteredAppointments().filter(a => a.status === 'NoShow').length
  );

  revenueGenerated = computed(() =>
    this.filteredAppointments()
      .filter(a => a.paymentStatus === 'Paid' || a.status === 'Completed')
      .reduce((sum, a) => sum + (a.fee ?? 0), 0)
  );

  statusMetrics = computed((): StatusMetric[] => [
    { label: 'Booked', count: this.bookedCount(), color: '#3b82f6' },
    { label: 'Completed', count: this.completedCount(), color: '#22c55e' },
    { label: 'Cancelled', count: this.cancelledCount(), color: '#ef4444' },
    { label: 'No Show', count: this.noShowCount(), color: '#f59e0b' }
  ]);

  statusDonutStyle = computed(() => {
    const metrics = this.statusMetrics();
    const total = metrics.reduce((sum, m) => sum + m.count, 0) || 1;
    let cursor = 0;
    const segments = metrics.map(metric => {
      const pct = (metric.count / total) * 100;
      const start = cursor;
      cursor += pct;
      return `${metric.color} ${start}% ${cursor}%`;
    });
    return `conic-gradient(${segments.join(', ')})`;
  });

  departmentTrends = computed(() => this.buildDepartmentTrends(this.filteredAppointments()));

  hasFilteredAppointments = computed(() => this.filteredAppointments().length > 0);

  ngOnInit(): void {
    this.loadAnalytics();
    this.loadExtendedAnalytics();
  }

  loadAnalytics(): void {
    this.loading.set(true);

    this.adminService.getDashboard()
      .subscribe({
        next: (response) => {
          this.dashboardData.set(response.data);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Failed to load appointment analytics.');
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
          this.extendedError.set('Failed to load extended appointment analytics.');
          this.toast.error('Failed to load extended appointment analytics.');
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

  getBarWidth(count: number): number {
    const stats = this.dashboardData()?.departmentStats ?? [];
    const max = Math.max(...stats.map((x: any) => x.appointmentCount), 1);
    return (count / max) * 100;
  }

  getTrendBarWidth(value: number, rows: DepartmentTrendRow[], key: keyof DepartmentTrendRow): number {
    const max = Math.max(...rows.map(r => Number(r[key]) || 0), 1);
    return ((Number(value) || 0) / max) * 100;
  }

  getStatusBarWidth(count: number): number {
    const max = Math.max(...this.statusMetrics().map(m => m.count), 1);
    return (count / max) * 100;
  }

  private buildDepartmentTrends(appointments: Appointment[]): DepartmentTrendRow[] {
    const map = new Map<string, DepartmentTrendRow>();

    for (const apt of appointments) {
      const dept = apt.departmentName || 'Unassigned';
      const row = map.get(dept) ?? {
        departmentName: dept,
        booked: 0,
        completed: 0,
        cancelled: 0,
        noShow: 0,
        revenue: 0
      };

      if (['Pending', 'Confirmed', 'PendingPayment', 'Rescheduled'].includes(apt.status)) {
        row.booked += 1;
      }
      if (apt.status === 'Completed') row.completed += 1;
      if (apt.status === 'Cancelled') row.cancelled += 1;
      if (apt.status === 'NoShow') row.noShow += 1;
      if (apt.paymentStatus === 'Paid' || apt.status === 'Completed') {
        row.revenue += apt.fee ?? 0;
      }

      map.set(dept, row);
    }

    return [...map.values()].sort((a, b) =>
      (b.completed + b.booked) - (a.completed + a.booked)
    );
  }
}
