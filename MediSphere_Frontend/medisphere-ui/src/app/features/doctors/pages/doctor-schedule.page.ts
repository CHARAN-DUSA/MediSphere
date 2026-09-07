import { Component, inject } from '@angular/core';
import { DoctorScheduleComponent } from '../doctor-schedule/doctor-schedule';
import { DoctorDashboardDataService } from '../../dashboard/doctor-dashboard/doctor-dashboard-data.service';

@Component({
  standalone: true,
  imports: [DoctorScheduleComponent],
  template: `
    <app-doctor-schedule
      [doctorId]="data.doctorIdNum()"
      (saveSchedule)="data.onSaveSchedule($event)"
      (blockSlot)="data.onBlockSlot($event)"
      (setVacation)="data.onSetVacation($event)">
    </app-doctor-schedule>
  `
})
export class DoctorSchedulePage
{
  readonly data = inject(DoctorDashboardDataService);
}
