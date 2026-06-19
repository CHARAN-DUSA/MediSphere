import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Review } from '../../../core/models/review.model';
import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';

@Component({
  selector: 'app-doctor-reviews',
  standalone: true,
  imports: [MsIconComponent, CommonModule, DatePipe, FormsModule],
  templateUrl: './doctor-reviews.html',
  styleUrls: ['./doctor-reviews.css']
})
export class DoctorReviewsComponent {
  reviews = input<Review[]>([]);
  respondToReview = output<{ reviewId: number; text: string }>();
}