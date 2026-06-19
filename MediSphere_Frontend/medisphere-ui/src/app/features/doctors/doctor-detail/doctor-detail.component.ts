import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
import { DoctorService } from '../../../core/services/doctor.service';
import { SavedDoctorsStateService } from '../../../core/services/saved-doctors-state.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Doctor } from '../../../core/models/doctor.model';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';

@Component({
  selector: 'app-doctor-detail',
  standalone: true,
  imports: [MsIconComponent, NgIf, RouterLink, LoaderComponent],
  templateUrl: './doctor-detail.html',
  styleUrls: ['./doctor-detail.css']
})
export class DoctorDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private doctorService = inject(DoctorService);
  private savedDoctors = inject(SavedDoctorsStateService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  doctor = signal<Doctor | null>(null);
  loading = signal(false);
  saving = signal(false);

  isPatient = () => this.auth.currentRole() === 'Patient';
  isSaved = () => {
    const doc = this.doctor();
    return doc ? this.savedDoctors.isSaved(doc.id) : false;
  };

  ngOnInit() {
    if (this.isPatient()) {
      this.savedDoctors.loadFavorites();
    }

    const id = +this.route.snapshot.paramMap.get('id')!;
    this.loading.set(true);
    this.doctorService.getDoctorById(id).subscribe(r => {
      this.doctor.set(r.data);
      this.loading.set(false);
    });
  }

  toggleSave() {
    const doc = this.doctor();
    if (!doc || !this.isPatient()) return;

    this.saving.set(true);
    this.savedDoctors.toggle(doc.id).subscribe({
      next: (response) => {
        this.toast.success(response.message || (response.data ? 'Doctor saved.' : 'Doctor removed from saved list.'));
        this.saving.set(false);
      },
      error: () => {
        this.toast.error('Unable to update saved doctors.');
        this.saving.set(false);
      }
    });
  }
}
