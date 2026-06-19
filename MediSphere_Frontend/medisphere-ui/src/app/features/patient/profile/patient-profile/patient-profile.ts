// src/app/features/patient/profile/patient-profile/patient-profile.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormsModule } from '@angular/forms';
import { FamilyMember, PatientProfile } from '../../../../core/models/patient-profile.model';
import { PatientService } from '../../../../core/services/patient.service';
import { ToastService } from '../../../../core/services/toast.service';


@Component({
  selector: 'app-patient-profile',
  standalone: true,
  imports: [NgFor, NgIf, ReactiveFormsModule, FormsModule],
  templateUrl: './patient-profile.html',
  styleUrls: ['./patient-profile.css']
})
export class PatientProfileComponent implements OnInit {
  private patientService = inject(PatientService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  familyMembers = signal<FamilyMember[]>([]);
  newMemberName = '';
  newMemberRelation = '';
  newMemberAge: number | null = null;

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
}