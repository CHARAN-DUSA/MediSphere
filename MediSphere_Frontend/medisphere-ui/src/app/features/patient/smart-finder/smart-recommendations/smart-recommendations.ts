import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Doctor } from '../../../../core/models/doctor.model';
import { LanguageService } from '../../../../core/services/language.service';
import { SmartRecommendService } from '../../../../core/services/smart-recommend.service';
import { SavedDoctorsStateService } from '../../../../core/services/saved-doctors-state.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-smart-recommendations',
  standalone: true,
  imports: [MsIconComponent, NgFor, NgIf, RouterLink, FormsModule],
  templateUrl: './smart-recommendations.html',
  styleUrls: ['./smart-recommendations.css']
})
export class SmartRecommendationsComponent implements OnInit {
  private smartRecommend = inject(SmartRecommendService);
  private savedDoctors = inject(SavedDoctorsStateService);
  private toast = inject(ToastService);
  langService = inject(LanguageService);

  searchingDoctors = signal(false);
  hasSearched = signal(false);
  recommendedDoctors = signal<Doctor[]>([]);
  savingDoctorId = signal<number | null>(null);
  symptomInput = '';
  symptomChips = ['chest pain', 'fever', 'skin rash', 'headache', 'back pain', 'pregnancy', 'child health', 'eye problem'];

  ngOnInit() {
    this.savedDoctors.loadFavorites();
  }

  isSaved(doctorId: number): boolean {
    return this.savedDoctors.isSaved(doctorId);
  }

  addChip(chip: string) {
    this.symptomInput = this.symptomInput ? `${this.symptomInput}, ${chip}` : chip;
  }

  findRecommendations() {
    if (!this.symptomInput.trim()) return;
    this.searchingDoctors.set(true);
    this.hasSearched.set(true);
    this.smartRecommend.getRecommendations(this.symptomInput.trim()).subscribe({
      next: (response) => {
        this.recommendedDoctors.set(response.data ?? []);
        this.searchingDoctors.set(false);
      },
      error: () => {
        this.toast.error('Recommendation engine error.');
        this.searchingDoctors.set(false);
      }
    });
  }

  toggleSave(doctorId: number, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.savingDoctorId.set(doctorId);
    this.savedDoctors.toggle(doctorId).subscribe({
      next: (response) => {
        this.toast.success(response.message || (response.data ? 'Doctor saved.' : 'Doctor removed from saved list.'));
        this.savingDoctorId.set(null);
      },
      error: () => {
        this.toast.error('Unable to update saved doctors.');
        this.savingDoctorId.set(null);
      }
    });
  }
}
