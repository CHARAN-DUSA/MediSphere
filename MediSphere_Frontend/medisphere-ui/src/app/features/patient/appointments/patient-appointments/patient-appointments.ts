import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
// src/app/features/patient/appointments/patient-appointments/patient-appointments.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { of } from 'rxjs';

import { FormsModule } from '@angular/forms';
import { Appointment } from '../../../../core/models/appointment.model';
import { AppointmentService } from '../../../../core/services/appointment.service';
import { AuthService } from '../../../../core/services/auth.service';
import { PaymentService } from '../../../../core/services/payment.service';
import { ReviewService } from '../../../../core/services/review.service';
import { ToastService } from '../../../../core/services/toast.service';
import { PaymentModalComponent } from '../../../shared/components/payment-modal/payment-modal';
import { ReviewModalComponent } from '../../../shared/components/review-modal/review-modal';

@Component({
  selector: 'app-patient-appointments',
  standalone: true,
  imports: [MsIconComponent, NgFor, NgIf, DatePipe, FormsModule, ReviewModalComponent, PaymentModalComponent],
  templateUrl: './patient-appointments.html',
  styleUrls: ['./patient-appointments.css']
})
export class PatientAppointmentsComponent implements OnInit
{
  private apptService = inject(AppointmentService);
  private reviewService = inject(ReviewService);
  private paymentService = inject(PaymentService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  apptFilter = signal('upcoming');
  appointments = signal<Appointment[]>([]);
  paymentProcessing = signal(false);
  selectedApptForReview = signal<Appointment | null>(null);

  // profile info for payment
  patientName = '';
  patientEmail = '';

  filteredAppointments = () =>
  {
    const f = this.apptFilter();
    return f === 'upcoming'
      ? this.appointments().filter(a => ['Pending', 'Confirmed', 'PendingPayment'].includes(a.status))
      : this.appointments().filter(a => ['Completed', 'Cancelled', 'NoShow'].includes(a.status));
  };

  ngOnInit()
  {
    this.apptService.getMyAppointments(1, 50).subscribe(r => this.appointments.set(r.data.items));
  }

  cancelAppt(id: number)
  {
    if (confirm('Cancel this appointment?'))
    {
      this.apptService.cancelAppointment(id).subscribe(() =>
      {
        this.toast.success('Appointment cancelled.');
        this.appointments.update(list => list.map(a => a.id === id ? { ...a, status: 'Cancelled' } : a));
      });
    }
  }

  printAppointment(a: Appointment)
  {
    const win = window.open('', '_blank');
    if (win)
    {
      win.document.write(`<html><body onload="window.print()"><h1>MediSphere</h1><p>Dr. ${a.doctorName}</p><p>Date: ${a.appointmentDate}</p><p>Token: #${a.queueToken}</p><p>Fee: ₹${a.fee}</p><p>Status: ${a.status}</p></body></html>`);
      win.document.close();
    }
  }

  openReviewModal(a: Appointment) { this.selectedApptForReview.set(a); }
  closeReviewModal() { this.selectedApptForReview.set(null); }

  onReviewSubmit(payload: { rating: number; comment: string })
  {
    const appt = this.selectedApptForReview();
    if (!appt) return;
    this.reviewService.createReview({ doctorId: appt.doctorId, appointmentId: appt.id, ...payload }).subscribe(() =>
    {
      this.toast.success('Review submitted. Pending approval.');
      this.closeReviewModal();
    });
  }

  async payForAppointment(appt: Appointment) {
  console.log('====================================');
  console.log('STEP 1: Pay button clicked');
  console.log('Appointment:', appt);
  console.log('====================================');

  this.paymentProcessing.set(true);

  this.paymentService.getPaymentConfig().subscribe({
    next: async (configResp) => {

      console.log('STEP 2: Payment config response');
      console.log(configResp);

      try {

        const config = configResp.data;

        console.log('STEP 3: Extracted config');
        console.log(config);

        console.log('STEP 4: Creating order');

        const order$ = this.paymentService.createOrder(
          appt.id,
          appt.fee
        );

        order$.subscribe({
          next: async (r) => {

            console.log('STEP 5: Create order response');
            console.log(r);

            const orderId = r.data;

            console.log('STEP 6: Order ID');
            console.log(orderId);

            console.log('STEP 7: Razorpay available?');
            console.log(!!(window as any).Razorpay);

            if (!config.isSandbox && (window as any).Razorpay) {

              try {

                console.log('STEP 8: Opening Razorpay');

                const paymentId =
                  await this.paymentService.launchRazorpayCheckout(
                    orderId,
                    appt.fee,
                    config.keyId,
                    this.patientName,
                    this.patientEmail
                  );

                console.log('STEP 9: Payment success');
                console.log(paymentId);

                this.paymentService
                  .simulateWebhook(
                    orderId,
                    paymentId,
                    appt.fee
                  )
                  .subscribe({
                    next: () => {
                      console.log('STEP 10: Webhook simulated');

                      this.toast.success(
                        'Payment successful!'
                      );

                      this.reload();

                      this.paymentProcessing.set(false);
                    },
                    error: (err) => {
                      console.error(
                        'STEP 10 FAILED:',
                        err
                      );

                      this.toast.error(
                        'Payment confirmation failed.'
                      );

                      this.paymentProcessing.set(false);
                    }
                  });

              } catch (err) {

                console.error(
                  'STEP 8 FAILED: Razorpay checkout error',
                  err
                );

                this.paymentService
                  .reportPaymentFailed(orderId)
                  .subscribe();

                this.toast.error(
                  'Payment cancelled or failed.'
                );

                this.paymentProcessing.set(false);
              }

            } else {

              console.log(
                'STEP 8B: Sandbox payment mode'
              );

              this.paymentService
                .simulateWebhook(
                  orderId,
                  `pay_sim_${Date.now()}`,
                  appt.fee
                )
                .subscribe({
                  next: () => {

                    console.log(
                      'STEP 9B: Sandbox payment success'
                    );

                    this.toast.success(
                      'Payment successful!'
                    );

                    this.reload();

                    this.paymentProcessing.set(false);
                  },
                  error: (err) => {

                    console.error(
                      'STEP 9B FAILED:',
                      err
                    );

                    this.toast.error(
                      'Payment simulation failed.'
                    );

                    this.paymentProcessing.set(false);
                  }
                });
            }
          },

          error: (err) => {

            console.error(
              'STEP 5 FAILED: createOrder error'
            );

            console.error(err);

            this.toast.error(
              'Failed to initialize payment order.'
            );

            this.paymentProcessing.set(false);
          }
        });

      } catch (err) {

        console.error(
          'STEP 3 FAILED: config parsing error'
        );

        console.error(err);

        this.toast.error(
          'Configuration error.'
        );

        this.paymentProcessing.set(false);
      }
    },

    error: (err) => {

      console.error(
        'STEP 2 FAILED: getPaymentConfig error'
      );

      console.error(err);

      this.toast.error(
        'Failed to load payment configuration.'
      );

      this.paymentProcessing.set(false);
    }
  });
}

  private reload()
  {
    this.apptService.getMyAppointments(1, 50).subscribe(r => this.appointments.set(r.data.items));
  }
}