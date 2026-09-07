import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
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

const PLATFORM_FEE_RATE     = 0.02;
const TAX_RATE               = 0.18;
const ADMIN_COMMISSION_RATE  = 0.15;
const DOCTOR_PAYOUT_RATE     =
  1 - PLATFORM_FEE_RATE - TAX_RATE - ADMIN_COMMISSION_RATE; // 0.65

export interface DepartmentPayoutRow {
  departmentName: string;
  appointmentCount: number;
  totalRevenue: number;
  payoutAmount: number;
  payoutSharePct: number;
  transactionCount: number;
}

export interface DoctorPayoutRow {
  doctorId: number;
  doctorName: string;
  departmentName: string;
  totalAppointments: number;
  totalRevenue: number;
  payoutAmount: number;
  payoutSharePct: number;
  transactionCount: number;
}

export interface PayoutTrendPoint {
  label: string;
  payout: number;
}

@Component({
  selector: 'app-payout-analytics',
  standalone: true,
  imports: [MsIconComponent, CommonModule, FormsModule],
  templateUrl: './payout-analytics.html',
  styleUrls: ['./payout-analytics.css']
})
export class PayoutAnalyticsComponent implements OnInit {
  private adminService = inject(AdminService);
  private appointmentService = inject(AppointmentService);
  private toast        = inject(ToastService);

  loading       = signal(false);
  dashboardData = signal<any>(null);

  extendedLoading = signal(false);
  extendedError   = signal<string | null>(null);
  appointments    = signal<Appointment[]>([]);

  period      = signal<AnalyticsPeriod>('month');
  customStart = signal('');
  customEnd   = signal('');

  doctorSearch     = signal('');
  doctorDeptFilter = signal('');

  readonly periodOptions: AnalyticsPeriod[] = ['today', 'week', 'month', 'year', 'custom'];
  periodLabel = periodLabel;

  gross            = computed(() => this.dashboardData()?.totalRevenue ?? 0);
  platformFee      = computed(() => +(this.gross() * PLATFORM_FEE_RATE).toFixed(2));
  tax              = computed(() => +(this.gross() * TAX_RATE).toFixed(2));
  adminCommission  = computed(() => +(this.gross() * ADMIN_COMMISSION_RATE).toFixed(2));
  doctorPayoutPool = computed(() => +(this.gross() * DOCTOR_PAYOUT_RATE).toFixed(2));
  totalDeductions  = computed(() =>
    +(this.platformFee() + this.tax() + this.adminCommission()).toFixed(2)
  );

  completedPayouts = computed(() => this.dashboardData()?.completedPayouts ?? 0);
  pendingPayouts   = computed(() => this.dashboardData()?.pendingPayouts   ?? 0);

  // % of the computed doctor payout pool
  completedPct = computed(() => {
    const pool = this.doctorPayoutPool();
    return pool > 0 ? +((this.completedPayouts() / pool) * 100).toFixed(1) : 0;
  });
  pendingPct = computed(() => {
    const pool = this.doctorPayoutPool();
    return pool > 0 ? +((this.pendingPayouts() / pool) * 100).toFixed(1) : 0;
  });

  // Health: pending < 30% of pool = good
  payoutHealthy = computed(() => this.pendingPct() < 30);

  // Net platform retained after doctor payout pool
  platformRetained = computed(() =>
    +(this.adminCommission() + this.platformFee()).toFixed(2)
  );

  readonly PLATFORM_FEE_PCT  = (PLATFORM_FEE_RATE    * 100).toFixed(0);
  readonly TAX_PCT            = (TAX_RATE             * 100).toFixed(0);
  readonly ADMIN_COMM_PCT     = (ADMIN_COMMISSION_RATE* 100).toFixed(0);
  readonly DOCTOR_PAYOUT_PCT  = (DOCTOR_PAYOUT_RATE   * 100).toFixed(0);

  private periodRange = computed(() =>
    resolvePeriodRange(this.period(), this.customStart(), this.customEnd())
  );

  private filteredAppointments = computed(() =>
    this.appointments().filter(a =>
      isDateInRange(a.appointmentDate, this.periodRange())
    )
  );

  departmentPayoutRows = computed(() =>
    this.buildDepartmentPayoutRows(this.filteredAppointments())
  );

  doctorPayoutRows = computed(() =>
    this.buildDoctorPayoutRows(this.filteredAppointments())
  );

  filteredDoctorPayoutRows = computed(() => {
    let rows = this.doctorPayoutRows();
    const query = this.doctorSearch().trim().toLowerCase();
    const dept = this.doctorDeptFilter();

    if (query) {
      rows = rows.filter(r => r.doctorName.toLowerCase().includes(query));
    }
    if (dept) {
      rows = rows.filter(r => r.departmentName === dept);
    }

    return [...rows].sort((a, b) => b.payoutAmount - a.payoutAmount);
  });

  departmentFilterOptions = computed(() => {
    const names = new Set(this.doctorPayoutRows().map(r => r.departmentName));
    return [...names].sort();
  });

  topDoctorPayoutChart = computed(() =>
    [...this.doctorPayoutRows()]
      .sort((a, b) => b.payoutAmount - a.payoutAmount)
      .slice(0, 8)
  );

  doctorPayoutTrend = computed(() =>
    this.buildDoctorPayoutTrend(this.filteredAppointments())
  );

  ngOnInit(): void {
    this.load();
    this.loadExtended();
  }

  load(): void {
    this.loading.set(true);
    this.adminService.getDashboard().subscribe({
      next: (r) => { this.dashboardData.set(r.data); this.loading.set(false); },
      error: () => { this.loading.set(false); this.toast.error('Failed to load payout analytics.'); }
    });
  }

  loadExtended(): void {
    this.extendedLoading.set(true);
    this.extendedError.set(null);

    this.appointmentService.getAllAppointments(1, 500).subscribe({
      next: (response) => {
        this.appointments.set(response.data?.items ?? []);
        this.extendedLoading.set(false);
      },
      error: () => {
        this.extendedLoading.set(false);
        this.extendedError.set('Failed to load payout detail data.');
        this.toast.error('Failed to load payout detail data.');
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
    this.loadExtended();
  }

  getBarWidth(value: number, rows: { payoutAmount: number }[]): number {
    const max = Math.max(...rows.map(r => r.payoutAmount), 1);
    return (value / max) * 100;
  }

  getDeptPayoutBarWidth(value: number, rows: DepartmentPayoutRow[]): number {
    const max = Math.max(...rows.map(r => r.payoutAmount), 1);
    return (value / max) * 100;
  }

  getTrendBarHeight(value: number, points: PayoutTrendPoint[]): number {
    const max = Math.max(...points.map(p => p.payout), 1);
    return (value / max) * 100;
  }

  private buildDepartmentPayoutRows(appointments: Appointment[]): DepartmentPayoutRow[] {
    const map = new Map<string, {
      appointmentCount: number;
      totalRevenue: number;
      transactionCount: number;
    }>();

    for (const apt of appointments) {
      const dept = apt.departmentName || 'Unassigned';
      const current = map.get(dept) ?? {
        appointmentCount: 0,
        totalRevenue: 0,
        transactionCount: 0
      };
      current.appointmentCount += 1;
      if (this.isPayableAppointment(apt)) {
        current.totalRevenue += apt.fee ?? 0;
        current.transactionCount += 1;
      }
      map.set(dept, current);
    }

    const rows = [...map.entries()].map(([departmentName, data]) => {
      const payoutAmount = +(data.totalRevenue * DOCTOR_PAYOUT_RATE).toFixed(2);
      return {
        departmentName,
        appointmentCount: data.appointmentCount,
        totalRevenue: data.totalRevenue,
        payoutAmount,
        payoutSharePct: 0,
        transactionCount: data.transactionCount
      };
    });

    const totalPayout = rows.reduce((sum, row) => sum + row.payoutAmount, 0);
    return rows
      .map(row => ({
        ...row,
        payoutSharePct: totalPayout > 0
          ? +((row.payoutAmount / totalPayout) * 100).toFixed(1)
          : 0
      }))
      .sort((a, b) => b.payoutAmount - a.payoutAmount);
  }

  private buildDoctorPayoutRows(appointments: Appointment[]): DoctorPayoutRow[] {
    const map = new Map<number, {
      doctorName: string;
      departmentName: string;
      totalAppointments: number;
      totalRevenue: number;
      transactionCount: number;
    }>();

    for (const apt of appointments) {
      const current = map.get(apt.doctorId) ?? {
        doctorName: apt.doctorName,
        departmentName: apt.departmentName || 'Unassigned',
        totalAppointments: 0,
        totalRevenue: 0,
        transactionCount: 0
      };
      current.totalAppointments += 1;
      if (this.isPayableAppointment(apt)) {
        current.totalRevenue += apt.fee ?? 0;
        current.transactionCount += 1;
      }
      map.set(apt.doctorId, current);
    }

    const rows = [...map.entries()].map(([doctorId, data]) => {
      const payoutAmount = +(data.totalRevenue * DOCTOR_PAYOUT_RATE).toFixed(2);
      return {
        doctorId,
        doctorName: data.doctorName,
        departmentName: data.departmentName,
        totalAppointments: data.totalAppointments,
        totalRevenue: data.totalRevenue,
        payoutAmount,
        payoutSharePct: 0,
        transactionCount: data.transactionCount
      };
    });

    const totalPayout = rows.reduce((sum, row) => sum + row.payoutAmount, 0);
    return rows.map(row => ({
      ...row,
      payoutSharePct: totalPayout > 0
        ? +((row.payoutAmount / totalPayout) * 100).toFixed(1)
        : 0
    }));
  }

  private buildDoctorPayoutTrend(appointments: Appointment[]): PayoutTrendPoint[] {
    const payable = appointments.filter(a => this.isPayableAppointment(a));
    const buckets = new Map<string, number>();

    for (const apt of payable) {
      const date = new Date(apt.appointmentDate);
      const label = `${date.getDate()} ${date.toLocaleString('en-IN', { month: 'short' })}`;
      buckets.set(label, (buckets.get(label) ?? 0) + ((apt.fee ?? 0) * DOCTOR_PAYOUT_RATE));
    }

    return [...buckets.entries()]
      .map(([label, payout]) => ({ label, payout: +payout.toFixed(2) }))
      .slice(-12);
  }

  private isPayableAppointment(apt: Appointment): boolean {
    return apt.paymentStatus === 'Paid'
      || apt.status === 'Completed'
      || apt.status === 'Confirmed';
  }
}
