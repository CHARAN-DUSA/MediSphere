import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppointmentAnalytics } from './appointment-analytics';

describe('AppointmentAnalytics', () => {
  let component: AppointmentAnalytics;
  let fixture: ComponentFixture<AppointmentAnalytics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentAnalytics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppointmentAnalytics);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
