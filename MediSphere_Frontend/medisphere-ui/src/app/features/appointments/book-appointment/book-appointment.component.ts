import { Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgFor, NgIf } from '@angular/common';
import { AppointmentService } from '../../../core/services/appointment.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { ToastService } from '../../../core/services/toast.service';
import { PaymentService } from '../../../core/services/payment.service';
import { Doctor } from '../../../core/models/doctor.model';
import { Appointment } from '../../../core/models/appointment.model';

@Component({
  selector: 'app-book-appointment',
  standalone: true,
  imports: [ReactiveFormsModule, NgFor, NgIf],
  templateUrl: './book-appointment.html',
  styleUrls: ['./book-appointment.css']
})
export class BookAppointmentComponent implements OnInit
{
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private appointmentService = inject(AppointmentService);
  private doctorService = inject(DoctorService);
  private toast = inject(ToastService);
  private paymentService = inject(PaymentService);

  doctor = signal<Doctor | null>(null);
  slots = signal<string[]>([]);
  selectedSlot = signal<string | null>(null);
  paymentProcessing = signal(false);
  pendingAmount = signal(0);
  rewardPoints = signal(0);
  loading = false;
  today = new Date().toISOString().split('T')[0];

  form = this.fb.group({
    appointmentDate: ['', Validators.required],
    reason: ['', [Validators.required, Validators.maxLength(500)]],
    isFollowUp: [false],
    useRewardPoints: [false]
  });

  ngOnInit()
  {
    const doctorId = +this.route.snapshot.paramMap.get('doctorId')!;
    this.doctorService.getDoctorById(doctorId).subscribe(r => this.doctor.set(r.data));
  }

  loadSlots()
  {
    const date = this.form.get('appointmentDate')?.value;
    if (!date || !this.doctor()) return;
    this.slots.set([]); this.selectedSlot.set(null);
    this.doctorService.getAvailableSlots(this.doctor()!.id, date).subscribe(r =>
    {
      this.slots.set(r.data.map((s: string) => s.substring(0, 5)));
    });
  }

  selectSlot(slot: string) { this.selectedSlot.set(slot); }

  onSubmit()
  {
    if (this.form.invalid || !this.selectedSlot()) return;
    this.loading = true;

    const dto = {
      doctorId: this.doctor()!.id,
      appointmentDate: this.form.value.appointmentDate!,
      startTime: this.selectedSlot()! + ':00',
      reason: this.form.value.reason!,
      isFollowUp: this.form.value.isFollowUp ?? false,
      useRewardPoints: this.form.value.useRewardPoints ?? false
    };

    this.appointmentService.createAppointment(dto).subscribe({
      next: async (r) =>
      {
        const appointment = r.data as Appointment;
        this.loading = false;

        // If appointment requires payment, launch checkout
        if (appointment.razorpayOrderId && appointment.fee > 0)
        {
          this.pendingAmount.set(appointment.fee);
          this.paymentProcessing.set(true);
          await this.processPayment(appointment);
        } else
        {
          // Free appointment or already confirmed
          this.toast.success('Appointment booked successfully!');
          this.router.navigate(['/appointments/history']);
        }
      },
      error: (error) =>
      {
        this.loading = false;

        console.log('FULL ERROR:', error);
        console.log('ERROR BODY:', error.error);

        this.toast.error(
          error?.error?.message ||
          JSON.stringify(error.error) ||
          'Booking failed'
        );
      }
    });
  }

  async processPayment(appointment: Appointment) {

  const orderId = appointment.razorpayOrderId!;
  const amount = appointment.fee;

  this.paymentService.getPaymentConfig().subscribe({

    next: async (configResp) => {

      const config = configResp.data;

      try {

        const paymentId =
          await this.paymentService.launchRazorpayCheckout(
            orderId,
            amount,
            config.keyId,
            '',
            ''
          );

        this.paymentService
          .simulateWebhook(orderId, paymentId, amount)
          .subscribe({
            next: () => {

              this.paymentProcessing.set(false);

              this.toast.success(
                'Payment successful!'
              );

              this.router.navigate([
                '/appointments/history'
              ]);
            },

            error: () => {

              this.paymentProcessing.set(false);

              this.toast.error(
                'Payment confirmation failed'
              );
            }
          });

      }
      catch (err) {

        console.error(err);

        this.paymentService
          .reportPaymentFailed(orderId)
          .subscribe();

        this.paymentProcessing.set(false);

        this.toast.error(
          'Payment cancelled.'
        );
      }
    },

    error: (err) => {

      console.error(err);

      this.paymentProcessing.set(false);

      this.toast.error(
        'Unable to load payment configuration.'
      );
    }
  });
}
}
