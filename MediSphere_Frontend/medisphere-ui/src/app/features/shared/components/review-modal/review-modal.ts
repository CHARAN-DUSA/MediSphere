import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
// src/app/features/shared/components/review-modal/review-modal.component.ts
import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { NgFor } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Appointment } from '../../../../core/models/appointment.model';


@Component({
  selector: 'app-review-modal',
  standalone: true,
  imports: [MsIconComponent, NgFor, FormsModule],
  templateUrl: './review-modal.html',
  styleUrls: ['./review-modal.css']
})
export class ReviewModalComponent
{
  @Input() appointment!: Appointment;
  @Output() closed = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<{ rating: number; comment: string }>();

  stars = [1, 2, 3, 4, 5];
  reviewRating = 5;
  reviewComment = '';

  submit()
  {
    this.submitted.emit({ rating: this.reviewRating, comment: this.reviewComment });
  }
}