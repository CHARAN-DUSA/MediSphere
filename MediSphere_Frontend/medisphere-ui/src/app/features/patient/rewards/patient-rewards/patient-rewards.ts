import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
// src/app/features/patient/rewards/patient-rewards/patient-rewards.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgIf, NgFor, DatePipe } from '@angular/common';
import { RewardsService, RewardStatement } from '../../../../core/services/rewards.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-patient-rewards',
  standalone: true,
  imports: [MsIconComponent, NgIf, NgFor, DatePipe],
  templateUrl: './patient-rewards.html',
  styleUrls: ['./patient-rewards.css']
})
export class PatientRewardsComponent implements OnInit {
  private rewardsService = inject(RewardsService);
  private toast = inject(ToastService);
  rewardStatement = signal<RewardStatement | null>(null);

  ngOnInit() {
    this.rewardsService.getMyStatement().subscribe(r => this.rewardStatement.set(r.data));
  }

  copyReferralCode() {
    const code = this.rewardStatement()?.referralCode;
    if (code) { navigator.clipboard.writeText(code); this.toast.success('Referral code copied!'); }
  }
}