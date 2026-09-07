import { Component, inject } from '@angular/core';
import { DoctorLogsComponent } from '../doctor-logs/doctor-logs';
import { DoctorDashboardDataService } from '../../dashboard/doctor-dashboard/doctor-dashboard-data.service';

@Component({
  standalone: true,
  imports: [DoctorLogsComponent],
  template: `
    <app-doctor-logs [appointments]="data.appointments()"></app-doctor-logs>
  `
})
export class DoctorPatientsPage
{
  readonly data = inject(DoctorDashboardDataService);
}
