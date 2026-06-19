import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { PatientService } from './patient.service';

@Injectable({ providedIn: 'root' })
export class SavedDoctorsStateService {
  private patientService = inject(PatientService);
  private auth = inject(AuthService);

  private favoriteIds = signal<Set<number>>(new Set());
  private loading = signal(false);

  readonly favorites = this.favoriteIds.asReadonly();
  readonly loadingFavorites = this.loading.asReadonly();

  loadFavorites(): void {
    if (this.auth.currentRole() !== 'Patient') {
      this.favoriteIds.set(new Set());
      return;
    }

    this.loading.set(true);
    this.patientService.getFavorites().subscribe({
      next: (response) => {
        this.favoriteIds.set(new Set(response.data.map((doctor) => doctor.id)));
        this.loading.set(false);
      },
      error: () => {
        this.favoriteIds.set(new Set());
        this.loading.set(false);
      }
    });
  }

  isSaved(doctorId: number): boolean {
    return this.favoriteIds().has(doctorId);
  }

  toggle(doctorId: number) {
    return this.patientService.toggleFavorite(doctorId).pipe(
      tap((response) => {
        this.favoriteIds.update((current) => {
          const next = new Set(current);
          if (response.data) {
            next.add(doctorId);
          } else {
            next.delete(doctorId);
          }
          return next;
        });
      })
    );
  }
}
