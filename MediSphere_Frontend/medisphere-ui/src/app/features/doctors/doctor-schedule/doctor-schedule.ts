import { Component, input, output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormsModule } from '@angular/forms';
import { inject } from '@angular/core';
import { BlockSlotDto, DoctorSchedule, VacationDto } from '../../../core/models/doctor.model';

@Component({
  selector: 'app-doctor-schedule',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './doctor-schedule.html',
  styleUrls: ['./doctor-schedule.css']
})
export class DoctorScheduleComponent implements OnInit {
  doctorId = input.required<number>();

  saveSchedule = output<DoctorSchedule[]>();
  blockSlot = output<BlockSlotDto>();
  setVacation = output<VacationDto>();

  private fb = inject(FormBuilder);

  dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  weeklySchedules: DoctorSchedule[] = [];

  blockForm = this.fb.group({
    date: ['', Validators.required],
    startTime: ['', Validators.required],
    reason: ['', Validators.required]
  });

  vacationForm = this.fb.group({
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    reason: ['', Validators.required]
  });

  ngOnInit() {
    this.initSchedule();
  }

  initSchedule() {
    this.weeklySchedules = [];
    for (let day = 0; day < 7; day++) {
      const isWeekend = day === 0 || day === 6;
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

  onSaveSchedule() {
    const formatted = this.weeklySchedules.map(s => ({
      ...s,
      startTime: s.startTime.split(':').length === 2 ? s.startTime + ':00' : s.startTime,
      endTime: s.endTime.split(':').length === 2 ? s.endTime + ':00' : s.endTime,
    }));
    this.saveSchedule.emit(formatted);
  }

  onBlockSlot() {
    if (this.blockForm.invalid) return;
    const val = this.blockForm.value;
    const dto: BlockSlotDto = {
      date: val.date!,
      startTime: val.startTime!.split(':').length === 2 ? val.startTime! + ':00' : val.startTime!,
      reason: val.reason!
    };
    this.blockSlot.emit(dto);
    this.blockForm.reset();
  }

  onSetVacation() {
    if (this.vacationForm.invalid) return;
    const val = this.vacationForm.value;
    const dto: VacationDto = {
      startDate: val.startDate!,
      endDate: val.endDate!,
      reason: val.reason!
    };
    this.setVacation.emit(dto);
    this.vacationForm.reset();
  }
}