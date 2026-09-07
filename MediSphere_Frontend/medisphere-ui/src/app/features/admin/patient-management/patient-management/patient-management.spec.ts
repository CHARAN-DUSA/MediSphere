import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PatientManagement } from './patient-management';

describe('PatientManagement', () => {
  let component: PatientManagement;
  let fixture: ComponentFixture<PatientManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientManagement]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PatientManagement);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
