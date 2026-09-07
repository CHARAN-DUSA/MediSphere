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
export class PatientAppointmentsComponent implements OnInit {
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

  apptToCancel = signal<Appointment | null>(null);
  cancelReasonType = 1;
  cancelReasonText = '';

  filteredAppointments = () => {
    const f = this.apptFilter();
    const visible = this.appointments().filter(a => !a.isBlockedSlot);
    return f === 'upcoming'
      ? visible.filter(a => ['Pending', 'Confirmed', 'PendingPayment'].includes(a.status))
      : visible.filter(a => ['Completed', 'Cancelled', 'NoShow'].includes(a.status));
  };

  ngOnInit() {
    this.apptService.getMyAppointments(1, 50).subscribe(r => this.appointments.set(r.data.items));
  }

  requestCancelAppt(a: Appointment) {
    this.cancelReasonType = 1;
    this.cancelReasonText = '';
    this.apptToCancel.set(a);
  }

  closeCancelModal() {
    this.apptToCancel.set(null);
  }

  confirmCancelAppt() {
    const appt = this.apptToCancel();
    if (!appt) return;

    const payload = {
      reasonType: this.cancelReasonType,
      reason: this.cancelReasonText.trim() || 'Patient requested cancellation'
    };

    this.apptService.cancelAppointment(appt.id, payload).subscribe({
      next: () => {
        this.toast.success('Appointment cancelled.');
        this.appointments.update(list => list.map(a => a.id === appt.id ? { 
          ...a, 
          status: 'Cancelled',
          paymentStatus: a.paymentStatus === 'Paid' ? 'Refunded' : a.paymentStatus,
          cancellationReason: payload.reason 
        } : a));
        this.closeCancelModal();
      },
      error: () => {
        this.toast.error('Failed to cancel appointment.');
      }
    });
  }

  printAppointment(a: Appointment) {
    const win = window.open('', '_blank');
    if (!win) return;

    const isCancelled = a.status === 'Cancelled';
    const isRefunded = a.paymentStatus === 'Refunded' || (a.refundStatus && a.refundStatus !== 'NotApplicable');

    win.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>MediSphere Receipt - #${a.id}</title>
        <style>
          body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; color: #1e293b; line-height: 1.5; }
          .header { text-align: center; border-bottom: 2px solid #0284c7; padding-bottom: 15px; margin-bottom: 25px; }
          .logo { font-size: 24px; font-weight: 800; color: #0284c7; letter-spacing: -0.5px; }
          .subtitle { font-size: 13px; color: #64748b; margin-top: 4px; }
          .badge { display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: 700; text-transform: uppercase; }
          .badge-confirmed { background: #ecfdf5; color: #047857; }
          .badge-cancelled { background: #fef2f2; color: #b91c1c; }
          table { width: 100%; border-collapse: collapse; margin-top: 20px; }
          th, td { text-align: left; padding: 12px; border-bottom: 1px solid #e2e8f0; font-size: 14px; }
          th { background: #f8fafc; color: #475569; width: 35%; }
          .refund-box { background: #f0fdf4; border: 1px solid #bbf7d0; padding: 15px; border-radius: 8px; margin-top: 20px; font-size: 13px; color: #166534; }
          .footer { margin-top: 40px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 15px; }
        </style>
      </head>
      <body onload="window.print()">
        <div class="header">
          <div class="logo">✚ MediSphere Healthcare Platform</div>
          <div class="subtitle">Official Consultation & Appointment Receipt</div>
        </div>

        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;">
          <div><strong>Appointment ID:</strong> #${a.id}</div>
          <div>
            <span class="badge ${isCancelled ? 'badge-cancelled' : 'badge-confirmed'}">${a.status}</span>
          </div>
        </div>

        <table>
          <tr><th>Doctor Name</th><td>${a.doctorName} (${a.departmentName || 'General'})</td></tr>
          <tr><th>Appointment Date & Time</th><td>${a.appointmentDate} at ${a.startTime}</td></tr>
          <tr><th>Token Number</th><td>#${a.queueToken || 'N/A'}</td></tr>
          <tr><th>Consultation Fee</th><td>₹${a.fee}</td></tr>
          <tr><th>Payment Status</th><td>${a.paymentStatus || 'Unpaid'}</td></tr>
          ${isCancelled ? `
            <tr><th>Cancelled By</th><td>${a.cancelledBy || 'Patient'}</td></tr>
            <tr><th>Cancellation Reason</th><td>${a.cancellationReason || a.reason || 'N/A'}</td></tr>
            <tr><th>Cancellation Date</th><td>${a.cancelledAt || 'N/A'}</td></tr>
          ` : `
            <tr><th>Reason for Visit</th><td>${a.reason || 'General Consultation'}</td></tr>
          `}
        </table>

        ${isCancelled && isRefunded ? `
          <div class="refund-box">
            <strong>REFUND CONFIRMATION</strong><br/>
            • Refund Status: <strong>${a.refundStatus || 'Refunded'}</strong><br/>
            • Refund Amount: <strong>₹${a.refundAmount || a.fee}</strong><br/>
            • Refund Destination: <strong>Original Payment Method / Bank Account</strong><br/>
            • Provider Reference: <strong>${a.razorpayRefundId || 'Simulated / Processed'}</strong>
          </div>
        ` : ''}

        <div class="footer">
          Thank you for choosing MediSphere. For assistance, contact support@medisphere.app.<br/>
          This is an electronically generated receipt and does not require a physical signature.
        </div>
      </body>
      </html>
    `);
    win.document.close();
  }

  openReviewModal(a: Appointment) { this.selectedApptForReview.set(a); }
  closeReviewModal() { this.selectedApptForReview.set(null); }

  onReviewSubmit(payload: { rating: number; comment: string }) {
    const appt = this.selectedApptForReview();
    if (!appt) return;
    this.reviewService.createReview({ doctorId: appt.doctorId, appointmentId: appt.id, ...payload }).subscribe(() => {
      this.toast.success('Review submitted. Pending approval.');
      this.appointments.update(list => list.map(a => a.id === appt.id ? { ...a, hasReviewed: true } : a));
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

  private reload() {
    this.apptService.getMyAppointments(1, 50).subscribe(r => this.appointments.set(r.data.items));
  }
}