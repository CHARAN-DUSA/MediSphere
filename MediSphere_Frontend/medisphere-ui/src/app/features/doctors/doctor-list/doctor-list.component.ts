import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { DoctorService } from '../../../core/services/doctor.service';
import { DepartmentService, Department } from '../../../core/services/department.service';
import { SavedDoctorsStateService } from '../../../core/services/saved-doctors-state.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Doctor } from '../../../core/models/doctor.model';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';

@Component({
  selector: 'app-doctor-list',
  standalone: true,
  imports: [MsIconComponent, NgFor, NgIf, RouterLink, ReactiveFormsModule, LoaderComponent],
  templateUrl: './doctor-list.html',
  styleUrls: ['./doctor-list.css']
})
export class DoctorListComponent implements OnInit {
  private doctorService = inject(DoctorService);
  private deptService = inject(DepartmentService);
  private savedDoctors = inject(SavedDoctorsStateService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private route = inject(ActivatedRoute);
  
  doctors = signal<Doctor[]>([]);
  departments = signal<Department[]>([]);
  loading = signal(false);
  showFilters = signal(false);
  page = signal(1);
  totalPages = signal(1);
  savingDoctorId = signal<number | null>(null);

  isPatient = () => this.auth.currentRole() === 'Patient';
  isSaved = (doctorId: number) => this.savedDoctors.isSaved(doctorId);
  
  searchCtrl = new FormControl('');
  deptCtrl = new FormControl('');
  specCtrl = new FormControl('');
  genderCtrl = new FormControl('');
  locationCtrl = new FormControl('');
  langCtrl = new FormControl('');
  minRatingCtrl = new FormControl('');
  maxFeeCtrl = new FormControl<number | null>(null);
  availCtrl = new FormControl(false);

  specialties = ['Cardiology','Neurology','Orthopedics','Pediatrics','Dermatology','Oncology','Psychiatry','General Medicine'];

  ngOnInit() {
    if (this.isPatient()) {
      this.savedDoctors.loadFavorites();
    }

    this.deptService.getAll().subscribe(r => this.departments.set(r.data));
    
    this.route.queryParams.subscribe(p => { 
      if (p['departmentId']) this.deptCtrl.setValue(p['departmentId']); 
    });

    // Subscribe to filter changes
    this.searchCtrl.valueChanges.pipe(debounceTime(400), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.deptCtrl.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.specCtrl.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.genderCtrl.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.locationCtrl.valueChanges.pipe(debounceTime(400), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.langCtrl.valueChanges.pipe(debounceTime(400), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.maxFeeCtrl.valueChanges.pipe(debounceTime(400), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.minRatingCtrl.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.availCtrl.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    
    this.load();
  }

  toggleFilters() {
    this.showFilters.update(v => !v);
  }

  load() {
    this.loading.set(true);
    this.doctorService.getDoctors({
      page: this.page(), 
      pageSize: 9,
      search: this.searchCtrl.value || undefined,
      departmentId: this.deptCtrl.value ? +this.deptCtrl.value : undefined,
      specialty: this.specCtrl.value || undefined,
      gender: this.genderCtrl.value || undefined,
      location: this.locationCtrl.value || undefined,
      language: this.langCtrl.value || undefined,
      maxFee: this.maxFeeCtrl.value !== null && this.maxFeeCtrl.value !== undefined ? this.maxFeeCtrl.value : undefined,
      minRating: this.minRatingCtrl.value ? +this.minRatingCtrl.value : undefined,
      isAvailable: this.availCtrl.value ? true : undefined
    }).subscribe(r => { 
      this.doctors.set(r.data.items); 
      this.totalPages.set(r.data.totalPages); 
      this.loading.set(false); 
    });
  }

  prevPage() { this.page.update(p => p - 1); this.load(); }
  nextPage() { this.page.update(p => p + 1); this.load(); }

  toggleSave(doctorId: number) {
    if (!this.isPatient()) return;
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
