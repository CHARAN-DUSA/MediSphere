import { Component, inject } from '@angular/core';
import { DoctorProfileComponent } from '../doctor-profile/doctor-profile';
import { DoctorDashboardDataService } from '../../dashboard/doctor-dashboard/doctor-dashboard-data.service';

@Component({
  standalone: true,
  imports: [DoctorProfileComponent],
  template: `
    <app-doctor-profile
      [doctorProfile]="data.doctorProfile()"
      [departments]="data.departments()"
      (profileUpdate)="data.onUpdateProfile($event)"
      (photoUpload)="data.uploadPhoto($event)">
    </app-doctor-profile>
  `
})
export class DoctorProfilePage
{
  readonly data = inject(DoctorDashboardDataService);
}
