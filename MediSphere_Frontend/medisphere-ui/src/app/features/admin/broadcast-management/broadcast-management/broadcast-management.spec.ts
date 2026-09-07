import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BroadcastManagement } from './broadcast-management';

describe('BroadcastManagement', () => {
  let component: BroadcastManagement;
  let fixture: ComponentFixture<BroadcastManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BroadcastManagement]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BroadcastManagement);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
