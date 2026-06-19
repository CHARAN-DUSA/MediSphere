import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsManagement } from './settings-management';

describe('SettingsManagement', () => {
  let component: SettingsManagement;
  let fixture: ComponentFixture<SettingsManagement>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SettingsManagement]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SettingsManagement);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
