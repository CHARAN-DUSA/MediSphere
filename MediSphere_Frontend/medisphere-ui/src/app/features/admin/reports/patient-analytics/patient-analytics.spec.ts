import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PatientAnalytics } from './patient-analytics';

describe('PatientAnalytics', () => {
  let component: PatientAnalytics;
  let fixture: ComponentFixture<PatientAnalytics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PatientAnalytics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PatientAnalytics);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
