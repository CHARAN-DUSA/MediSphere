// src/app/features/patient/profile/patient-profile/patient-profile.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { FamilyMember, PatientProfile } from '../../../../core/models/patient-profile.model';
import { PatientService } from '../../../../core/services/patient.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-patient-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './patient-profile.html',
  styleUrls: ['./patient-profile.css']
})
export class PatientProfileComponent implements OnInit {
  private patientService = inject(PatientService);
  private authService = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private fb = inject(FormBuilder);

  familyMembers = signal<FamilyMember[]>([]);
  newMemberName = '';
  newMemberRelation = '';
  newMemberAge: number | null = null;

  // Account deletion signals
  showDeleteModal = signal(false);
  deleting = signal(false);
  deleteReason = '';

  profileForm = this.fb.group({
    firstName: [''], lastName: [''], phoneNumber: [''],
    address: [''], bloodGroup: [''], medicalHistory: ['']
  });

  ngOnInit() {
    this.patientService.getProfile().subscribe(r => {
      if (r.data) {
        this.profileForm.patchValue({
          firstName: r.data.firstName, lastName: r.data.lastName,
          phoneNumber: r.data.phoneNumber, address: r.data.address,
          bloodGroup: r.data.bloodGroup, medicalHistory: r.data.medicalHistory
        });
        this.familyMembers.set(r.data.familyMembers || []);
      }
    });
  }

  onUpdateProfile() {
    const val = this.profileForm.value;
    const dto: PatientProfile = {
      firstName: val.firstName || '', lastName: val.lastName || '',
      phoneNumber: val.phoneNumber || '', address: val.address || '',
      bloodGroup: val.bloodGroup || '', medicalHistory: val.medicalHistory || '',
      familyMembers: this.familyMembers()
    };
    this.patientService.updateProfile(dto).subscribe(() => this.toast.success('Profile updated.'));
  }

  addFamilyMember() {
    if (!this.newMemberName || !this.newMemberRelation || this.newMemberAge === null) {
      this.toast.error('Please fill all family member fields.'); return;
    }
    const dto: FamilyMember = { id: 0, name: this.newMemberName, relation: this.newMemberRelation, age: this.newMemberAge };
    this.patientService.addFamilyMember(dto).subscribe({
      next: (res) => {
        this.familyMembers.update(list => [...list, res]);
        this.toast.success('Family member added.');
        this.newMemberName = ''; this.newMemberRelation = ''; this.newMemberAge = null;
      }
    });
  }

  removeFamilyMember(member: FamilyMember) {
    this.patientService.deleteFamilyMember(member.id).subscribe(() => {
      this.familyMembers.update(list => list.filter(x => x.id !== member.id));
      this.toast.success('Family member removed.');
    });
  }

  openDeleteModal() {
    this.deleteReason = '';
    this.showDeleteModal.set(true);
  }

  closeDeleteModal() {
    this.showDeleteModal.set(false);
  }

  confirmDeleteAccount() {
    if (this.deleting()) return;
    this.deleting.set(true);

    const reason = this.deleteReason.trim() || 'Patient requested account closure';
    this.authService.deleteAccount(reason).subscribe({
      next: (res) => {
        this.deleting.set(false);
        this.showDeleteModal.set(false);
        this.toast.success(res?.message || 'Your account has been deleted and data anonymized.');
        this.authService.logout();
      },
      error: (err) => {
        this.deleting.set(false);
        this.toast.error(err?.error?.message || 'Failed to delete account. Please contact support.');
      }
    });
  }
}