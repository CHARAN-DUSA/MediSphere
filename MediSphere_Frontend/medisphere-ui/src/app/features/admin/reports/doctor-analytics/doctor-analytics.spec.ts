import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DoctorAnalytics } from './doctor-analytics';

describe('DoctorAnalytics', () => {
  let component: DoctorAnalytics;
  let fixture: ComponentFixture<DoctorAnalytics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DoctorAnalytics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DoctorAnalytics);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
