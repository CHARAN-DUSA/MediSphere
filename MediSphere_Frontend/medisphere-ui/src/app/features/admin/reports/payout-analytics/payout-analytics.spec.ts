import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PayoutAnalytics } from './payout-analytics';

describe('PayoutAnalytics', () => {
  let component: PayoutAnalytics;
  let fixture: ComponentFixture<PayoutAnalytics>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PayoutAnalytics]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PayoutAnalytics);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
