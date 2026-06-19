import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Review } from '../../../../core/models/review.model';
import { ReviewService } from '../../../../core/services/review.service';
import { ToastService } from '../../../../core/services/toast.service';



@Component({
  selector: 'app-review-moderation',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MsIconComponent
  ],
  templateUrl: './review-moderation.html',
  styleUrls: ['./review-moderation.css']
})
export class ReviewModerationComponent implements OnInit {

  private reviewService = inject(ReviewService);
  private toast = inject(ToastService);

  loading = signal(false);

  pendingReviews = signal<Review[]>([]);

  ngOnInit(): void {
    this.loadPendingReviews();
  }

  loadPendingReviews(): void {

    this.loading.set(true);

    this.reviewService.getPendingReviews()
      .subscribe({
        next: (response) => {

          this.pendingReviews.set(
            response.data ?? []
          );

          this.loading.set(false);
        },

        error: () => {

          this.loading.set(false);

          this.toast.error(
            'Failed to load reviews.'
          );
        }
      });
  }

  moderateReview(
    id: number,
    status: string
  ): void {

    this.reviewService
      .moderateReview(id, status)
      .subscribe({
        next: (response) => {

          this.toast.success(
            response.message ||
            `Review ${status.toLowerCase()} successfully.`
          );

          this.loadPendingReviews();
        },

        error: () => {

          this.toast.error(
            'Failed to moderate review.'
          );
        }
      });
  }
}