import { Component, input, output, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormsModule } from '@angular/forms';
import { Doctor } from '../../../core/models/doctor.model';
import { Department } from '../../../core/services/department.service';
import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';

@Component({
  selector: 'app-doctor-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, MsIconComponent],
  templateUrl: './doctor-profile.html',
  styleUrls: ['./doctor-profile.css']
})
export class DoctorProfileComponent implements OnChanges
{
  doctorProfile = input<Doctor | null>(null);
  departments = input<Department[]>([]);

  profileUpdate = output<any>();
  photoUpload = output<File>();

  private fb = inject(FormBuilder);

  photoFile: File | null = null;

  profileForm = this.fb.group({
    firstName: [''], lastName: [''], phoneNumber: [''],
    consultationFee: [0], specialty: [''], qualification: [''],
    experienceYears: [0], departmentId: [0], gender: [''],
    location: [''], languagesSpoken: [''], isAvailable: [true], bio: ['']
  });

  ngOnChanges(changes: SimpleChanges)
  {
    if (changes['doctorProfile'] && this.doctorProfile())
    {
      const d = this.doctorProfile()!;
      this.profileForm.patchValue({
        firstName: d.firstName, lastName: d.lastName,
        phoneNumber: d.phoneNumber, consultationFee: d.consultationFee,
        specialty: d.specialty, qualification: d.qualification,
        experienceYears: d.experienceYears, departmentId: d.departmentId,
        gender: d.gender, location: d.location,
        languagesSpoken: d.languagesSpoken, isAvailable: d.isAvailable, bio: d.bio
      });
    }
  }

  onPhotoSelected(event: any)
  {
    const file = event.target.files[0];
    if (file) this.photoFile = file;
  }

  onUploadPhoto()
  {
    if (this.photoFile)
    {
      this.photoUpload.emit(this.photoFile);
      this.photoFile = null;
    }
  }

  onSubmit()
  {
    const val = this.profileForm.value;
    this.profileUpdate.emit({
      firstName: val.firstName || '', lastName: val.lastName || '',
      phoneNumber: val.phoneNumber || '', consultationFee: val.consultationFee || 0,
      specialty: val.specialty || '', qualification: val.qualification || '',
      experienceYears: val.experienceYears || 0,
      departmentId: val.departmentId ? +val.departmentId : 0,
      gender: val.gender || '', location: val.location || '',
      languagesSpoken: val.languagesSpoken || '',
      isAvailable: val.isAvailable === null ? true : !!val.isAvailable,
      bio: val.bio || ''
    });
  }
}