import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Appointment } from '../../../core/models/appointment.model';

@Component({
  selector: 'app-doctor-logs',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule],
  templateUrl: './doctor-logs.html',
  styleUrls: ['./doctor-logs.css']
})
export class DoctorLogsComponent
{
  appointments = input<Appointment[]>([]);

  searchQuery = '';
  statusFilter = '';
  filteredLogs = signal<Appointment[]>([]);

  constructor()
  {
    effect(() =>
    {
      // Re-filter whenever appointments input changes
      this.applyFilter();
    });
  }

  applyFilter()
  {
    let list = this.appointments();
    if (this.searchQuery)
    {
      const q = this.searchQuery.toLowerCase();
      list = list.filter(a => a.patientName.toLowerCase().includes(q));
    }
    if (this.statusFilter)
    {
      list = list.filter(a => a.status === this.statusFilter);
    }
    this.filteredLogs.set(list);
  }
}