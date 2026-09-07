import { Component, inject } from '@angular/core';
import { DoctorTodayComponent } from '../doctor-today/doctor-today';
import { DoctorDashboardDataService } from '../../dashboard/doctor-dashboard/doctor-dashboard-data.service';

@Component({
  standalone: true,
  imports: [DoctorTodayComponent],
  template: `
    <app-doctor-today
      [todayAppts]="data.todayAppts()"
      [liveQueueWaiting]="data.liveQueueWaiting()"
      [currentlyServing]="data.currentlyServing()"
      [signalRConnected]="data.signalRService.queueConnected()"
      (statusUpdate)="data.updateStatus($event.id, $event.status)"
      (openNotes)="data.openConsultationModal($event)"
      (callNext)="data.callNext()"
      (queueStatusChange)="data.changeQueueStatus($event.id, $event.status)">
    </app-doctor-today>
  `
})
export class DoctorAppointmentsPage
{
  readonly data = inject(DoctorDashboardDataService);
}
