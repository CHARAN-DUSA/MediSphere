import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DoctorManagement } from './doctor-management';

describe('DoctorManagement', () => {
  let component: DoctorManagement;
  let fixture: ComponentFixture<DoctorManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DoctorManagement]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DoctorManagement);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
