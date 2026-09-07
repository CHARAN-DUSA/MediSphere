import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, AdminRefund, AdminRefundDetail, AdminRefundSummary } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-refund-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './refund-management.html',
  styleUrls: ['./refund-management.css']
})
export class RefundManagementComponent implements OnInit {
  private adminService = inject(AdminService);
  private toast = inject(ToastService);

  loading = signal(false);
  refunds = signal<AdminRefund[]>([]);
  summary = signal<AdminRefundSummary | null>(null);

  // Filters & Pagination
  searchQuery = signal('');
  selectedStatus = signal('');
  fromDate = signal('');
  toDate = signal('');
  page = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);
  totalPages = signal(1);

  // Detail Modal & Retry
  selectedRefundDetail = signal<AdminRefundDetail | null>(null);
  retryingRefundId = signal<number | null>(null);

  ngOnInit(): void {
    this.loadSummary();
    this.loadRefunds();
  }

  loadSummary(): void {
    this.adminService.getRefundSummary().subscribe({
      next: (res: any) => {
        if (res?.data) {
          this.summary.set(res.data);
        }
      },
      error: () => {
        console.error('Failed to load refund summary metrics');
      }
    });
  }

  loadRefunds(): void {
    this.loading.set(true);
    this.adminService.getRefunds(
      this.searchQuery().trim(),
      this.selectedStatus(),
      this.fromDate(),
      this.toDate(),
      this.page(),
      this.pageSize()
    ).subscribe({
      next: (res: any) => {
        this.loading.set(false);
        if (res?.data) {
          this.refunds.set(res.data.items || []);
          this.totalCount.set(res.data.totalCount || 0);
          this.totalPages.set(Math.ceil((res.data.totalCount || 0) / this.pageSize()) || 1);
        }
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load refund records');
      }
    });
  }

  onFilterChange(): void {
    this.page.set(1);
    this.loadRefunds();
  }

  goToPage(p: number): void {
    if (p >= 1 && p <= this.totalPages() && p !== this.page()) {
      this.page.set(p);
      this.loadRefunds();
    }
  }

  viewRefundDetails(refund: AdminRefund): void {
    this.adminService.getRefundDetail(refund.appointmentId).subscribe({
      next: (res: any) => {
        if (res?.data) {
          this.selectedRefundDetail.set(res.data);
        } else {
          this.toast.error('Could not retrieve refund details');
        }
      },
      error: () => {
        this.toast.error('Failed to load refund dossier');
      }
    });
  }

  closeDetailsModal(): void {
    this.selectedRefundDetail.set(null);
  }

  retryRefund(appointmentId: number): void {
    if (this.retryingRefundId()) return;

    this.retryingRefundId.set(appointmentId);
    this.adminService.retryRefund(appointmentId).subscribe({
      next: (res: any) => {
        this.retryingRefundId.set(null);
        if (res?.isSuccess) {
          this.toast.success(res.message || 'Refund successfully re-processed!');
          this.loadSummary();
          this.loadRefunds();
          if (this.selectedRefundDetail()?.appointmentId === appointmentId) {
            this.viewRefundDetails({ appointmentId } as AdminRefund);
          }
        } else {
          this.toast.error(res?.message || 'Refund retry failed');
        }
      },
      error: (err: any) => {
        this.retryingRefundId.set(null);
        this.toast.error(err.error?.message || 'Server error while retrying refund');
      }
    });
  }

  getStatusClass(status: string): string {
    switch ((status || '').toLowerCase()) {
      case 'refunded':
        return 'status-refunded';
      case 'refundprocessing':
      case 'processing':
        return 'status-processing';
      case 'refundrequested':
        return 'status-requested';
      case 'refundfailed':
      case 'failed':
        return 'status-failed';
      case 'simulated':
        return 'status-simulated';
      default:
        return 'status-na';
    }
  }

  formatStatus(status: string): string {
    switch (status) {
      case 'Refunded': return 'Refunded';
      case 'RefundProcessing': return 'Processing (In Transit)';
      case 'RefundRequested': return 'Requested / Pending';
      case 'RefundFailed': return 'Failed';
      case 'Simulated': return 'Simulated (Sandbox)';
      case 'PartiallyRefunded': return 'Partially Refunded';
      default: return status || 'N/A';
    }
  }
}
