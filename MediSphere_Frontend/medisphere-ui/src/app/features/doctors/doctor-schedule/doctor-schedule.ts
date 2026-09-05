import {
  Component,
  input,
  output,
  OnInit,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  FormsModule
} from '@angular/forms';

import {
  BlockSlotDto,
  DoctorSchedule,
  DailyScheduleSlot,
  VacationDto
} from '../../../core/models/doctor.model';

import { DoctorService } from '../../../core/services/doctor.service';

@Component({
  selector: 'app-doctor-schedule',
  standalone: true,

  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule
  ],

  templateUrl: './doctor-schedule.html',
  styleUrls: ['./doctor-schedule.css']
})
export class DoctorScheduleComponent implements OnInit {

  // ============================================================
  // INPUT
  // ============================================================

  doctorId = input.required<number>();


  // ============================================================
  // OUTPUTS
  // ============================================================

  saveSchedule = output<DoctorSchedule[]>();

  blockSlot = output<BlockSlotDto>();

  setVacation = output<VacationDto>();


  // ============================================================
  // SERVICES
  // ============================================================

  private fb = inject(FormBuilder);

  private doctorService = inject(DoctorService);


  // ============================================================
  // DATA
  // ============================================================

  dayNames = [
    'Sunday',
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday'
  ];

  weeklySchedules: DoctorSchedule[] = [];

  /**
   * Full schedule returned by backend.
   *
   * Every slot is included:
   *
   * available
   * booked
   * blocked
   * vacation
   */
  dateSlots: DailyScheduleSlot[] = [];

  selectedDate = this.getToday();


  // ============================================================
  // BLOCK SLOT FORM
  // ============================================================

  blockForm = this.fb.group({

    date: [
      this.selectedDate,
      Validators.required
    ],

    startTime: [
      '',
      Validators.required
    ],

    reason: [
      '',
      Validators.required
    ]

  });


  // ============================================================
  // VACATION FORM
  // ============================================================

  vacationForm = this.fb.group({

    startDate: [
      '',
      Validators.required
    ],

    endDate: [
      '',
      Validators.required
    ],

    reason: [
      '',
      Validators.required
    ]

  });


  // ============================================================
  // INIT
  // ============================================================

  ngOnInit(): void {

    this.initSchedule();

    this.loadDateSchedule();

  }


  // ============================================================
  // DEFAULT WEEKLY SCHEDULE
  // ============================================================

  initSchedule(): void {

    this.weeklySchedules = [];

    for (let day = 0; day < 7; day++) {

      const isWeekend =
        day === 0 ||
        day === 6;

      this.weeklySchedules.push({

        doctorId: this.doctorId(),

        dayOfWeek: day,

        startTime: '09:00:00',

        endTime: '17:00:00',

        slotDurationMinutes: 30,

        isActive: !isWeekend

      });

    }

  }


  // ============================================================
  // LOAD DAILY SCHEDULE
  // ============================================================

  loadDateSchedule(): void {

  if (!this.selectedDate) {
    return;
  }

  this.doctorService
    .getDailySchedule(
      this.doctorId(),
      this.selectedDate
    )
    .subscribe({

      next: response => {

        this.dateSlots =
          response.data ?? [];

        console.log(
          'Daily schedule:',
          this.dateSlots
        );

      },

      error: error => {

        console.error(
          'Failed to load daily schedule',
          error
        );

        this.dateSlots = [];

      }

    });

}


  // ============================================================
  // DATE CHANGED
  // ============================================================

  onDateChanged(): void {

    if (!this.selectedDate) {
      return;
    }

    /*
     * Keep block form date synchronized.
     */
    this.blockForm.patchValue({
      date: this.selectedDate
    });

    /*
     * Reload complete schedule.
     */
    this.loadDateSchedule();

  }


  // ============================================================
  // BLOCK EXISTING SLOT
  // ============================================================

  blockExistingSlot(slot: DailyScheduleSlot): void {
  if (slot.status !== 'Available') {
    return;
  }

  const date = slot.date.substring(0, 10);
  const startTime = this.normalizeTime(slot.startTime).substring(0, 5);

  this.blockForm.patchValue({
    date: date,
    startTime: startTime,
    reason: ''
  });

  document.querySelector('.schedule-block-card')?.scrollIntoView({
    behavior: 'smooth',
    block: 'center'
  });
}


  // ============================================================
  // DELETE BLOCKED SLOT
  // ============================================================

  deleteBlockedSlot(
    slot: DailyScheduleSlot
  ): void {

    /*
     * A blocked slot must have an
     * appointment ID because your
     * backend stores the block as an
     * Appointment record.
     */
    if (
      !slot.appointmentId
    ) {
      return;
    }

    const confirmed =
      window.confirm(
        `Remove the block for ${this.formatTime(
          slot.startTime
        )}?`
      );

    if (!confirmed) {
      return;
    }

    this.doctorService
      .deleteBlockedSlot(
        this.doctorId(),
        slot.appointmentId
      )
      .subscribe({

        next: () => {

          /*
           * Backend is the source of truth.
           * Reload after successful deletion.
           */
          this.loadDateSchedule();

        },

        error: err => {

          console.error(
            'Failed to delete blocked slot',
            err
          );

        }

      });

  }


  // ============================================================
  // SAVE WEEKLY SCHEDULE
  // ============================================================

  onSaveSchedule(): void {

    const formatted =
      this.weeklySchedules.map(
        schedule => ({

          ...schedule,

          startTime:
            this.normalizeTime(
              schedule.startTime
            ),

          endTime:
            this.normalizeTime(
              schedule.endTime
            )

        })
      );

    /*
     * Parent component can perform
     * the actual API save.
     */
    this.saveSchedule.emit(
      formatted
    );

  }


  // ============================================================
  // BLOCK SLOT
  // ============================================================

  onBlockSlot(): void {

    if (
      this.blockForm.invalid
    ) {
      this.blockForm.markAllAsTouched();
      return;
    }

    const value =
      this.blockForm.value;

    const startTime =
      this.normalizeTime(
        value.startTime!
      );

    const dto: BlockSlotDto = {

      date:
        value.date!,

      startTime,

      reason:
        value.reason!.trim()

    };

    /*
     * Do NOT push the slot locally.
     *
     * Backend will create the block.
     * After the parent/API completes,
     * the daily schedule should be
     * reloaded.
     */
    this.blockSlot.emit(dto);

    /*
     * Reset form.
     */
    this.blockForm.reset({

      date:
        this.selectedDate,

      startTime: '',

      reason: ''

    });

  }


  // ============================================================
  // SET VACATION
  // ============================================================

  onSetVacation(): void {

    if (
      this.vacationForm.invalid
    ) {
      this.vacationForm.markAllAsTouched();
      return;
    }

    const value =
      this.vacationForm.value;

    const startDate =
      value.startDate!;

    const endDate =
      value.endDate!;

    /*
     * Basic validation.
     */
    if (
      startDate > endDate
    ) {

      this.vacationForm
        .get('endDate')
        ?.setErrors({
          invalidRange: true
        });

      return;
    }

    const dto: VacationDto = {

      startDate,

      endDate,

      reason:
        value.reason!.trim()

    };

    /*
     * Parent performs vacation API call.
     */
    this.setVacation.emit(dto);

    /*
     * Reset form after emitting.
     */
    this.vacationForm.reset();

  }


  // ============================================================
  // TIME HELPERS
  // ============================================================

  private timeToMinutes(
    time: string
  ): number {

    if (!time) {
      return 0;
    }

    const parts =
      time.split(':');

    return (
      Number(parts[0]) * 60 +
      Number(parts[1])
    );

  }


  private minutesToTime(
    minutes: number
  ): string {

    const hours =
      Math.floor(
        minutes / 60
      );

    const mins =
      minutes % 60;

    return (
      `${String(hours).padStart(2, '0')}:` +
      `${String(mins).padStart(2, '0')}:00`
    );

  }


  private normalizeTime(
    time: string
  ): string {

    if (!time) {
      return '';
    }

    /*
     * Convert HH:mm -> HH:mm:ss
     */
    if (time.length === 5) {

      return `${time}:00`;

    }

    return time;

  }


  private calculateSlotEnd(
    date: string,
    startTime: string
  ): string {

    const day =
      new Date(
        `${date}T00:00:00`
      );

    const schedule =
      this.weeklySchedules.find(
        schedule =>
          schedule.dayOfWeek ===
          day.getDay()
      );

    const duration =
      schedule?.slotDurationMinutes ??
      30;

    const start =
      this.timeToMinutes(
        startTime
      );

    return this.minutesToTime(
      start + duration
    );

  }


  // ============================================================
  // DISPLAY TIME
  // ============================================================

  formatTime(
    time: string
  ): string {

    if (!time) {
      return '';
    }

    const [
      hour,
      minute
    ] =
      time.split(':');

    const h =
      Number(hour);

    const suffix =
      h >= 12
        ? 'PM'
        : 'AM';

    const displayHour =
      h % 12 || 12;

    return (
      `${displayHour}:${minute} ${suffix}`
    );

  }


  // ============================================================
  // TODAY
  // ============================================================

  private getToday(): string {

    const now =
      new Date();

    const year =
      now.getFullYear();

    const month =
      String(
        now.getMonth() + 1
      ).padStart(2, '0');

    const day =
      String(
        now.getDate()
      ).padStart(2, '0');

    return (
      `${year}-${month}-${day}`
    );

  }


  // ============================================================
  // STATISTICS
  // ============================================================

  get availableSlotCount(): number {

    return this.dateSlots.filter(
      slot =>
        slot.status ===
        'Available'
    ).length;

  }


  get bookedSlotCount(): number {

    return this.dateSlots.filter(
      slot =>
        slot.status ===
        'Booked'
    ).length;

  }


  get blockedSlotCount(): number {

    return this.dateSlots.filter(
      slot =>
        slot.status ===
          'Blocked' ||
        slot.status ===
          'Vacation'
    ).length;

  }


  // ============================================================
  // SELECTED DATE CLOSED
  // ============================================================

  get isSelectedDateClosed(): boolean {

    const date =
      new Date(
        `${this.selectedDate}T00:00:00`
      );

    const schedule =
      this.weeklySchedules.find(
        schedule =>
          schedule.dayOfWeek ===
          date.getDay()
      );

    return !schedule?.isActive;

  }

}