import { Component, inject } from '@angular/core';
import { DoctorReviewsComponent } from '../doctor-reviews/doctor-reviews';
import { DoctorDashboardDataService } from '../../dashboard/doctor-dashboard/doctor-dashboard-data.service';

@Component({
  standalone: true,
  imports: [DoctorReviewsComponent],
  template: `
    <app-doctor-reviews
      [reviews]="data.reviews()"
      (respondToReview)="data.submitReviewResponse($event.reviewId, $event.text)">
    </app-doctor-reviews>
  `
})
export class DoctorReviewsPage
{
  readonly data = inject(DoctorDashboardDataService);
}
