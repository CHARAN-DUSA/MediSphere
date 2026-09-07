import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';

const PLATFORM_FEE_RATE  = 0.02;  // 2%
const TAX_RATE           = 0.18;  // 18% GST / TDS
const ADMIN_COMMISSION_RATE = 0.15; // 15%
const DOCTOR_PAYOUT_RATE =
  1 - PLATFORM_FEE_RATE - TAX_RATE - ADMIN_COMMISSION_RATE; // 65%

@Component({
  selector: 'app-revenue-report',
  standalone: true,
  imports: [MsIconComponent, CommonModule],
  templateUrl: './revenue-report.html',
  styleUrls: ['./revenue-report.css']
})
export class RevenueReportComponent implements OnInit {
  private adminService = inject(AdminService);
  private toast        = inject(ToastService);

  loading       = signal(false);
  dashboardData = signal<any>(null);

  // ── Derived amounts ───────────────────────────────────
  gross = computed(() => this.dashboardData()?.totalRevenue ?? 0);

  platformFee = computed(() =>
    +(this.gross() * PLATFORM_FEE_RATE).toFixed(2)
  );
  tax = computed(() =>
    +(this.gross() * TAX_RATE).toFixed(2)
  );
  adminCommission = computed(() =>
    +(this.gross() * ADMIN_COMMISSION_RATE).toFixed(2)
  );
  doctorPayout = computed(() =>
    +(this.gross() * DOCTOR_PAYOUT_RATE).toFixed(2)
  );
  totalDeductions = computed(() =>
    +(this.platformFee() + this.tax() + this.adminCommission()).toFixed(2)
  );

  // ── Bar widths (% of gross) ───────────────────────────
  platformFeeWidth    = computed(() => PLATFORM_FEE_RATE    * 100);
  taxWidth            = computed(() => TAX_RATE             * 100);
  adminCommissionWidth= computed(() => ADMIN_COMMISSION_RATE* 100);
  doctorPayoutWidth   = computed(() => DOCTOR_PAYOUT_RATE   * 100);

  // ── Payout breakdown ─────────────────────────────────
  completedPayouts = computed(() =>
    this.dashboardData()?.completedPayouts ?? 0
  );
  pendingPayouts = computed(() =>
    this.dashboardData()?.pendingPayouts ?? 0
  );

  completedRate = computed(() => {
    const d = this.doctorPayout();
    return d > 0 ? +((this.completedPayouts() / d) * 100).toFixed(1) : 0;
  });
  pendingRate = computed(() => {
    const d = this.doctorPayout();
    return d > 0 ? +((this.pendingPayouts() / d) * 100).toFixed(1) : 0;
  });

  // ── Dept bar widths ───────────────────────────────────
  deptBarWidth(count: number): number {
    const stats = this.dashboardData()?.departmentStats ?? [];
    const max   = Math.max(...stats.map((d: any) => d.appointmentCount), 1);
    return (count / max) * 100;
  }

  readonly PLATFORM_FEE_PCT   = (PLATFORM_FEE_RATE   * 100).toFixed(0);
  readonly TAX_PCT             = (TAX_RATE            * 100).toFixed(0);
  readonly ADMIN_COMM_PCT      = (ADMIN_COMMISSION_RATE * 100).toFixed(0);
  readonly DOCTOR_PAYOUT_PCT   = (DOCTOR_PAYOUT_RATE  * 100).toFixed(0);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminService.getDashboard().subscribe({
      next: (r) => { this.dashboardData.set(r.data); this.loading.set(false); },
      error: () => { this.loading.set(false); this.toast.error('Failed to load revenue report.'); }
    });
  }
}