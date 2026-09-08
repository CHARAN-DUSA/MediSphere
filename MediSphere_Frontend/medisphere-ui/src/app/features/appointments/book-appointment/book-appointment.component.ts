import
  {
    Component,
    inject,
    OnInit,
    signal
  } from '@angular/core';

import
  {
    ReactiveFormsModule,
    FormBuilder,
    Validators
  } from '@angular/forms';

import
  {
    ActivatedRoute,
    Router
  } from '@angular/router';

import
  {
    NgFor,
    NgIf
  } from '@angular/common';

import
  {
    AppointmentService
  } from '../../../core/services/appointment.service';

import
  {
    AppointmentSlot,
    DoctorService
  } from '../../../core/services/doctor.service';

import
  {
    ToastService
  } from '../../../core/services/toast.service';

import
  {
    PaymentService
  } from '../../../core/services/payment.service';

import
  {
    AuthService
  } from '../../../core/services/auth.service';

import
  {
    Doctor
  } from '../../../core/models/doctor.model';

import
  {
    Appointment
  } from '../../../core/models/appointment.model';
import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';


@Component({
  selector: 'app-book-appointment',

  standalone: true,

  imports: [
    MsIconComponent,
    ReactiveFormsModule,
    NgFor,
    NgIf
  ],

  templateUrl: './book-appointment.html',

  styleUrls: ['./book-appointment.css']
})
export class BookAppointmentComponent
  implements OnInit
{


  private fb =
    inject(FormBuilder);

  private route =
    inject(ActivatedRoute);

  private router =
    inject(Router);

  private appointmentService =
    inject(AppointmentService);

  private doctorService =
    inject(DoctorService);

  private toast =
    inject(ToastService);

  private paymentService =
    inject(PaymentService);

  private auth =
    inject(AuthService);


  /* ========================================
     DOCTOR
  ======================================== */

  doctor =
    signal<Doctor | null>(null);


  /* ========================================
     SLOTS
  ======================================== */

  /*
   * Keep the complete slot object so the UI
   * can distinguish:
   *
   * Available
   * Booked
   * Blocked
   * Vacation
   */
  slots =
    signal<AppointmentSlot[]>([]);


  selectedSlot =
    signal<string | null>(null);


  /* ========================================
     PAYMENT
  ======================================== */

  paymentProcessing =
    signal(false);

  pendingAmount =
    signal(0);

  rewardPoints =
    signal(0);


  loading = false;


  /* ========================================
     DATE
  ======================================== */

  today =
    new Date()
      .toISOString()
      .split('T')[0];


  /* ========================================
     FORM
  ======================================== */

  form =
    this.fb.group({

      appointmentDate: [
        '',
        Validators.required
      ],

      reason: [
        '',
        [
          Validators.required,
          Validators.maxLength(500)
        ]
      ],

      isFollowUp: [
        false
      ],

      useRewardPoints: [
        false
      ]

    });


  /* ========================================
     ROLE
  ======================================== */

  isPatient =
    () =>
      this.auth.currentRole() === 'Patient';


  /* ========================================
     INITIALIZATION
  ======================================== */

  ngOnInit()
  {

    /*
     * HARD FRONTEND SECURITY GUARD
     *
     * Doctors and other non-patient users
     * cannot enter the booking workflow.
     */
    if (!this.isPatient())
    {

      this.toast.error(
        'Only patients can book appointments.'
      );

      this.router.navigate([
        '/doctors'
      ]);

      return;

    }


    const doctorId =
      +this.route.snapshot
        .paramMap
        .get('doctorId')!;


    this.doctorService
      .getDoctorById(doctorId)
      .subscribe({

        next: r =>
        {

          this.doctor.set(
            r.data
          );

        },

        error: () =>
        {

          this.toast.error(
            'Unable to load doctor details.'
          );

          this.router.navigate([
            '/doctors'
          ]);

        }

      });

  }


  /* ========================================
     LOAD SLOTS
  ======================================== */

  loadSlots()
  {

    const date =
      this.form
        .get('appointmentDate')
        ?.value;


    if (
      !date ||
      !this.doctor()
    )
    {

      return;

    }


    this.slots.set([]);

    this.selectedSlot.set(null);


    this.doctorService
      .getAvailableSlots(
        this.doctor()!.id,
        date
      )
      .subscribe({

        next: r =>
        {

          this.slots.set(
            r.data ?? []
          );

        },

        error: error =>
        {

          console.error(
            'Failed to load appointment slots:',
            error
          );


          this.slots.set([]);

          this.selectedSlot.set(
            null
          );


          this.toast.error(

            error?.error?.message ||

            'Unable to load appointment slots.'

          );

        }

      });

  }


  /* ========================================
     SELECT SLOT
  ======================================== */

  selectSlot(
    slot: AppointmentSlot
  )
  {

    /*
     * Only Available slots can be selected.
     */
    if (
      slot.status !== 'Available'
    )
    {

      return;

    }


    this.selectedSlot.set(

      slot.startTime
        .substring(0, 5)

    );

  }


  /* ========================================
     SUBMIT BOOKING
  ======================================== */

  onSubmit()
  {

    /*
     * SECOND FRONTEND ROLE GUARD
     *
     * Prevent submission even if this method
     * is somehow triggered manually.
     */
    if (!this.isPatient())
    {

      this.toast.error(
        'Only patients can book appointments.'
      );

      this.router.navigate([
        '/doctors'
      ]);

      return;

    }


    if (this.form.invalid || !this.selectedSlot() || !this.doctor())
    {
      this.form.markAllAsTouched();

      if (this.form.get('reason')?.hasError('required'))
      {
        this.toast.error('Please enter a reason for your visit.');
      }

      return;
    }


    this.loading = true;


    const dto = {

      doctorId:
        this.doctor()!.id,

      appointmentDate:
        this.form.value
          .appointmentDate!,

      startTime:
        this.selectedSlot() +
        ':00',

      reason:
        this.form.value
          .reason!,

      isFollowUp:
        this.form.value
          .isFollowUp ??
        false,

      useRewardPoints:
        this.form.value
          .useRewardPoints ??
        false

    };


    this.appointmentService
      .createAppointment(dto)
      .subscribe({

        next: async (r) =>
        {

          const appointment =
            r.data as Appointment;


          this.loading = false;


          /*
           * Payment required
           */
          if (
            appointment.razorpayOrderId &&
            appointment.fee > 0
          )
          {

            this.pendingAmount.set(
              appointment.fee
            );

            this.paymentProcessing.set(
              true
            );


            await this.processPayment(
              appointment
            );

          }

          else
          {

            /*
             * Free appointment or
             * already confirmed.
             */

            this.toast.success(
              'Appointment booked successfully!'
            );


            this.router.navigate([
              '/appointments/history'
            ]);

          }

        },


        error: (error) =>
        {

          this.loading = false;


          console.log(
            'FULL ERROR:',
            error
          );

          console.log(
            'ERROR BODY:',
            error.error
          );


          this.toast.error(

            error?.error?.message ||

            JSON.stringify(
              error.error
            ) ||

            'Booking failed'

          );


          /*
           * Refresh slots because another
           * patient may have booked it.
           */
          this.loadSlots();

        }

      });

  }


  /* ========================================
     PAYMENT
  ======================================== */

  async processPayment(
    appointment: Appointment
  )
  {

    const orderId =
      appointment
        .razorpayOrderId!;

    const amount =
      appointment.fee;


    this.paymentService
      .getPaymentConfig()
      .subscribe({

        next: async (
          configResp
        ) =>
        {

          const config =
            configResp.data;


          try
          {

            const paymentId =
              await this.paymentService
                .launchRazorpayCheckout(

                  orderId,

                  amount,

                  config.keyId,

                  '',

                  ''

                );


            this.paymentService
              .simulateWebhook(

                orderId,

                paymentId,

                amount

              )
              .subscribe({

                next: () =>
                {

                  this.paymentProcessing.set(
                    false
                  );


                  this.toast.success(
                    'Payment successful!'
                  );


                  this.router.navigate([
                    '/appointments/history'
                  ]);

                },


                error: () =>
                {

                  this.paymentProcessing.set(
                    false
                  );


                  this.toast.error(
                    'Payment confirmation failed'
                  );

                }

              });

          }

          catch (err)
          {

            console.error(err);


            this.paymentService
              .reportPaymentFailed(
                orderId
              )
              .subscribe();


            this.paymentProcessing.set(
              false
            );


            this.toast.error(
              'Payment cancelled.'
            );

          }

        },


        error: (err) =>
        {

          console.error(err);


          this.paymentProcessing.set(
            false
          );


          this.toast.error(
            'Unable to load payment configuration.'
          );

        }

      });

  }

}