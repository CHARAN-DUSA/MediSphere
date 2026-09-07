import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReviewModeration } from './review-moderation';

describe('ReviewModeration', () => {
  let component: ReviewModeration;
  let fixture: ComponentFixture<ReviewModeration>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReviewModeration]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReviewModeration);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
