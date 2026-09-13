import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DOCUMENT, NgIf } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';
import { DoctorService, AppointmentSlot } from '../../../core/services/doctor.service';
import { SavedDoctorsStateService } from '../../../core/services/saved-doctors-state.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

import { Doctor } from '../../../core/models/doctor.model';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';


@Component({
  selector: 'app-doctor-detail',
  standalone: true,

  imports: [
    MsIconComponent,
    NgIf,
    RouterLink,
    LoaderComponent
  ],

  templateUrl: './doctor-detail.html',

  styleUrls: ['./doctor-detail.css']
})
export class DoctorDetailComponent
  implements OnInit, OnDestroy
{


  // ============================================================
  // SERVICES
  // ============================================================
  private title = inject(Title);

  private meta = inject(Meta);

  private document = inject(DOCUMENT);
  private route =
    inject(ActivatedRoute);

  private doctorService =
    inject(DoctorService);

  private savedDoctors =
    inject(SavedDoctorsStateService);

  private auth =
    inject(AuthService);

  private toast =
    inject(ToastService);


  // ============================================================
  // DOCTOR STATE
  // ============================================================

  doctor =
    signal<Doctor | null>(null);

  loading =
    signal(false);

  saving =
    signal(false);


  // ============================================================
  // AVAILABILITY STATE
  //
  // today  = doctor has an available slot today
  // future = no slot today, but future slot exists
  // none   = no available slot found
  // ============================================================

  availabilityState =
    signal<'today' | 'future' | 'none'>('none');


  /*
   * Example:
   *
   * Sep 7, 2026
   */
  nextAvailableDate =
    signal<string | null>(null);


  /*
   * Number of future days to check.
   *
   * We check the next 30 days after today.
   */
  private readonly availabilityDays = 30;


  // ============================================================
  // PROFILE IMAGE
  // ============================================================

  cardImageUrl:
    string | null = null;

  selectedProfileImageUrl:
    string | null = null;


  // ============================================================
  // ROLE
  // ============================================================

  isPatient =
    () => this.auth.currentRole() === 'Patient';


  // ============================================================
  // SAVED DOCTOR
  // ============================================================

  isSaved =
    () =>
    {

      const doc =
        this.doctor();

      return doc
        ? this.savedDoctors.isSaved(doc.id)
        : false;

    };

  // ============================================================
  // CANONICAL URL
  // ============================================================

  private setCanonicalUrl(url: string): void
  {
    let canonical =
      this.document.querySelector(
        'link[rel="canonical"]'
      ) as HTMLLinkElement | null;


    if (!canonical)
    {
      canonical =
        this.document.createElement('link');

      canonical.setAttribute(
        'rel',
        'canonical'
      );

      this.document.head.appendChild(
        canonical
      );
    }


    canonical.setAttribute(
      'href',
      url
    );
  }
  // ============================================================
  // INITIALIZATION
  // ============================================================

  ngOnInit(): void
  {

    /*
     * Favorites are only relevant to patients.
     */
    if (this.isPatient())
    {

      this.savedDoctors.loadFavorites();

    }


    const id =
      +this.route.snapshot
        .paramMap
        .get('id')!;


    if (!id)
    {

      this.toast.error(
        'Invalid doctor.'
      );

      return;

    }


    this.loading.set(true);


    // ========================================================
    // LOAD DOCTOR
    // ========================================================

    this.doctorService
      .getDoctorById(id)
      .subscribe({

        next: response =>
        {

          const doctor =
            response.data;


          // ==================================================
          // STORE DOCTOR
          // ==================================================

          this.doctor.set(
            doctor
          );


          // ==================================================
          // SEO METADATA
          // ==================================================

          const fullName =
            `Dr. ${doctor.firstName} ${doctor.lastName}`;

          const specialty =
            doctor.specialty ||
            'Medical Specialist';

          const department =
            doctor.departmentName ||
            'Healthcare';

          const location =
            doctor.location
              ? ` in ${doctor.location}`
              : '';

          const pageTitle =
            `${fullName} – ${specialty}${location} | MediSphere`;

          const pageDescription =
            `View ${fullName}'s profile on MediSphere. ` +
            `${fullName} is a ${specialty} in ${department}${location}. ` +
            `Explore qualifications, experience, consultation fee, ` +
            `ratings, availability, and appointment information.`;


          this.title.setTitle(
            pageTitle
          );


          this.meta.updateTag({
            name: 'description',
            content: pageDescription
          });


          this.meta.updateTag({
            name: 'robots',
            content: 'index, follow'
          });


          // ==================================================
          // OPEN GRAPH
          // ==================================================

          this.meta.updateTag({
            property: 'og:title',
            content: pageTitle
          });


          this.meta.updateTag({
            property: 'og:description',
            content: pageDescription
          });


          this.meta.updateTag({
            property: 'og:type',
            content: 'profile'
          });


          this.meta.updateTag({
            property: 'og:url',
            content:
              `https://medi-sphere-dun.vercel.app/doctors/${doctor.id}`
          });


          // ==================================================
          // CANONICAL URL
          // ==================================================

          this.setCanonicalUrl(
            `https://medi-sphere-dun.vercel.app/doctors/${doctor.id}`
          );


          this.loading.set(false);


          /*
           * IMPORTANT:
           *
           * Do NOT use doctor.isAvailable here.
           *
           * Availability on the detail page is based
           * on actual appointment slots.
           */
          this.loadAvailability(
            doctor.id
          );


          // ==================================================
          // LOAD PROFILE IMAGE
          // ==================================================

          if (
            doctor?.profileImageUrl
          )
          {

            this.doctorService
              .getProfileImageBlob(
                doctor.id
              )
              .subscribe({

                next: blob =>
                {

                  /*
                   * Release old blob URL.
                   */
                  if (
                    this.cardImageUrl &&
                    this.cardImageUrl.startsWith('blob:')
                  )
                  {

                    URL.revokeObjectURL(
                      this.cardImageUrl
                    );

                  }


                  this.cardImageUrl =
                    URL.createObjectURL(blob);

                },


                error: () =>
                {

                  this.cardImageUrl =
                    null;

                }

              });

          }

        },


        error: error =>
        {

          console.error(
            'Failed to load doctor details:',
            error
          );


          this.loading.set(false);


          this.toast.error(
            'Unable to load doctor details.'
          );

        }

      });

  }


  // ============================================================
  // AVAILABILITY
  // ============================================================

  /**
   * Determines the availability state for the doctor.
   *
   * 1. Check today's slots.
   * 2. If there is an Available slot today:
   *      state = today
   *
   * 3. Otherwise search future dates.
   * 4. If a future Available slot exists:
   *      state = future
   *
   * 5. Otherwise:
   *      state = none
   */
  private loadAvailability(
    doctorId: number
  ): void
  {

    /*
     * Reset previous state.
     */
    this.availabilityState.set(
      'none'
    );

    this.nextAvailableDate.set(
      null
    );


    const today =
      new Date();


    const todayString =
      this.formatApiDate(
        today
      );


    // ========================================================
    // CHECK TODAY
    // ========================================================

    this.doctorService
      .getAvailableSlots(
        doctorId,
        todayString
      )
      .subscribe({

        next: response =>
        {

          const slots =
            response.data ?? [];


          /*
           * Only a slot with status "Available"
           * counts as an appointment opportunity.
           *
           * Booked / Blocked / Vacation do not count.
           */
          const availableToday =
            slots.some(
              slot =>
                slot.status === 'Available'
            );


          if (availableToday)
          {

            this.availabilityState.set(
              'today'
            );

            this.nextAvailableDate.set(
              null
            );

            return;

          }


          /*
           * Nothing available today.
           *
           * Search future dates.
           */
          this.findNextAvailableDate(
            doctorId,
            today
          );

        },


        error: error =>
        {

          console.error(
            'Unable to load today availability:',
            error
          );


          /*
           * If today's request fails, still try
           * future dates rather than immediately
           * showing "No appointments available".
           */
          this.findNextAvailableDate(
            doctorId,
            today
          );

        }

      });

  }


  // ============================================================
  // FUTURE AVAILABILITY
  // ============================================================

  /**
   * Searches the next 30 days for an Available slot.
   *
   * The first available future date is displayed.
   */
  private findNextAvailableDate(
    doctorId: number,
    today: Date
  ): void
  {

    let daysChecked =
      0;


    const checkNextDay =
      (): void =>
      {


        /*
         * No availability found within
         * the configured search period.
         */
        if (
          daysChecked >=
          this.availabilityDays
        )
        {

          this.availabilityState.set(
            'none'
          );

          this.nextAvailableDate.set(
            null
          );

          return;

        }


        daysChecked++;


        const futureDate =
          new Date(today);


        futureDate.setDate(
          today.getDate() +
          daysChecked
        );


        const dateString =
          this.formatApiDate(
            futureDate
          );


        // ====================================================
        // CHECK FUTURE DATE
        // ====================================================

        this.doctorService
          .getAvailableSlots(
            doctorId,
            dateString
          )
          .subscribe({

            next: response =>
            {

              const slots =
                response.data ?? [];


              /*
               * Look specifically for an Available slot.
               */
              const hasAvailableSlot =
                slots.some(
                  slot =>
                    slot.status === 'Available'
                );


              if (hasAvailableSlot)
              {

                /*
                 * We found the first future
                 * appointment date.
                 */
                this.availabilityState.set(
                  'future'
                );


                this.nextAvailableDate.set(
                  this.formatDisplayDate(
                    futureDate
                  )
                );


                return;

              }


              /*
               * Nothing on this date.
               *
               * Check tomorrow.
               */
              checkNextDay();

            },


            error: error =>
            {

              console.warn(
                `Unable to check availability for ${dateString}`,
                error
              );


              /*
               * Continue searching even if
               * one date fails.
               */
              checkNextDay();

            }

          });

      };


    checkNextDay();

  }


  // ============================================================
  // API DATE FORMAT
  // ============================================================

  /**
   * Converts Date into:
   *
   * YYYY-MM-DD
   *
   * Example:
   * 2026-09-07
   *
   * We intentionally don't use toISOString()
   * because that converts the date to UTC and can
   * cause date shifting around midnight.
   */
  private formatApiDate(
    date: Date
  ): string
  {

    const year =
      date.getFullYear();


    const month =
      String(
        date.getMonth() + 1
      ).padStart(
        2,
        '0'
      );


    const day =
      String(
        date.getDate()
      ).padStart(
        2,
        '0'
      );


    return `${year}-${month}-${day}`;

  }


  // ============================================================
  // DISPLAY DATE
  // ============================================================

  /**
   * Converts:
   *
   * 2026-09-07
   *
   * into:
   *
   * Sep 7, 2026
   */
  private formatDisplayDate(
    date: Date
  ): string
  {

    return date.toLocaleDateString(
      'en-US',
      {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      }
    );

  }


  // ============================================================
  // OPEN PROFILE IMAGE
  // ============================================================

  openProfileImage(
    doctorId: number
  ): void
  {

    if (!doctorId)
    {
      return;
    }


    this.doctorService
      .getProfileImageBlob(
        doctorId
      )
      .subscribe({

        next: blob =>
        {

          /*
           * Release previous preview URL.
           */
          if (
            this.selectedProfileImageUrl &&
            this.selectedProfileImageUrl.startsWith('blob:')
          )
          {

            URL.revokeObjectURL(
              this.selectedProfileImageUrl
            );

          }


          this.selectedProfileImageUrl =
            URL.createObjectURL(blob);

        },


        error: error =>
        {

          console.error(
            'Failed to load doctor profile image',
            error
          );


          this.selectedProfileImageUrl =
            null;

        }

      });

  }


  // ============================================================
  // CLOSE PROFILE IMAGE
  // ============================================================

  closeProfileImage(): void
  {

    if (
      this.selectedProfileImageUrl &&
      this.selectedProfileImageUrl.startsWith('blob:')
    )
    {

      URL.revokeObjectURL(
        this.selectedProfileImageUrl
      );

    }


    this.selectedProfileImageUrl =
      null;

  }


  // ============================================================
  // SAVE / UNSAVE
  // ============================================================

  toggleSave(): void
  {

    const doc =
      this.doctor();


    /*
     * Only patients can save doctors.
     */
    if (
      !doc ||
      !this.isPatient()
    )
    {

      return;

    }


    this.saving.set(true);


    this.savedDoctors
      .toggle(doc.id)
      .subscribe({

        next: response =>
        {

          this.toast.success(

            response.message ||

            (
              response.data
                ? 'Doctor saved.'
                : 'Doctor removed from saved list.'
            )

          );


          this.saving.set(false);

        },


        error: error =>
        {

          console.error(
            'Unable to update saved doctor:',
            error
          );


          this.toast.error(
            'Unable to update saved doctors.'
          );


          this.saving.set(false);

        }

      });

  }


  // ============================================================
  // DESTROY
  // ============================================================

  ngOnDestroy(): void
  {

    /*
     * Release header avatar blob.
     */
    if (
      this.cardImageUrl &&
      this.cardImageUrl.startsWith('blob:')
    )
    {

      URL.revokeObjectURL(
        this.cardImageUrl
      );

    }


    /*
     * Release full-size image blob.
     */
    if (
      this.selectedProfileImageUrl &&
      this.selectedProfileImageUrl.startsWith('blob:')
    )
    {

      URL.revokeObjectURL(
        this.selectedProfileImageUrl
      );

    }

  }

}