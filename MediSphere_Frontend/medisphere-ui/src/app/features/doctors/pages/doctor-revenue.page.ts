import { Component } from '@angular/core';
import { DoctorEarningsComponent } from '../doctor-earnings/doctor-earnings';

@Component({
  standalone: true,
  imports: [DoctorEarningsComponent],
  template: `<app-doctor-earnings></app-doctor-earnings>`
})
export class DoctorRevenuePage {}
