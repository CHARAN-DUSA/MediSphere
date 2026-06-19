import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Doctor } from '../../../../core/models/doctor.model';
import { SavedDoctorsStateService } from '../../../../core/services/saved-doctors-state.service';
import { PatientService } from '../../../../core/services/patient.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-favorite-doctors',
  standalone: true,
  imports: [MsIconComponent, NgFor, NgIf, RouterLink],
  templateUrl: './favorite-doctors.html',
  styleUrls: ['./favorite-doctors.css']
})
export class FavoriteDoctorsComponent implements OnInit {
  private patientService = inject(PatientService);
  private savedDoctors = inject(SavedDoctorsStateService);
  private toast = inject(ToastService);

  favorites = signal<Doctor[]>([]);
  loading = signal(true);
  removingId = signal<number | null>(null);

  ngOnInit() {
    this.loadFavorites();
  }

  loadFavorites() {
    this.loading.set(true);
    this.patientService.getFavorites().subscribe({
      next: (response) => {
        this.favorites.set(response.data ?? []);
        this.savedDoctors.loadFavorites();
        this.loading.set(false);
      },
      error: () => {
        this.toast.error('Unable to load saved doctors.');
        this.loading.set(false);
      }
    });
  }

  removeFavorite(doctorId: number) {
    this.removingId.set(doctorId);
    this.savedDoctors.toggle(doctorId).subscribe({
      next: (response) => {
        this.toast.success(response.message || 'Removed from saved doctors.');
        this.favorites.update(list => list.filter(d => d.id !== doctorId));
        this.removingId.set(null);
      },
      error: () => {
        this.toast.error('Unable to remove saved doctor.');
        this.removingId.set(null);
      }
    });
  }
}
