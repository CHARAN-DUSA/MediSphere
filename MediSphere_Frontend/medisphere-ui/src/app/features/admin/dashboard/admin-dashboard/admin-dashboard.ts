import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [MsIconComponent, 
    CommonModule
  ],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css']
})
export class AdminDashboardComponent implements OnInit
{

  private adminService = inject(AdminService);
  private toast = inject(ToastService);
  private router = inject(Router);

  loading = signal(false);

  stats = signal<any | null>(null);

  ngOnInit(): void
  {
    this.loadDashboard();
  }
  constructor()
  {
    console.log('Dashboard Component Created');
  }
  goTo(page: string): void
  {
    const routeMap: Record<string, string> = {
      departments: 'department-management'
    };
    this.router.navigate(['/admin', routeMap[page] ?? page]);
  }

  loadDashboard(): void
  {

    this.loading.set(true);

    this.adminService
      .getDashboard()
      .subscribe({
        next: (response) =>
        {

          this.stats.set(
            response.data
          );

          this.loading.set(false);
        },

        error: () =>
        {

          this.loading.set(false);

          this.toast.error(
            'Failed to load dashboard statistics.'
          );
        }
      });
  }

  barWidth(count: number): number
  {

    const counts =
      this.stats()?.departmentStats?.map(
        (d: any) => d.appointmentCount
      ) ?? [1];

    const max =
      Math.max(...counts, 1);

    return (count / max) * 100;
  }
}