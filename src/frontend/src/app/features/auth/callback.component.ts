import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';

import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-callback',
  imports: [LoadingSpinnerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <app-loading-spinner label="Completing sign-in..." />
    </section>
  `,
  styles: [
    `
      .page {
        padding: 4rem;
        display: flex;
        justify-content: center;
      }
    `,
  ],
})
export class CallbackComponent implements OnInit {
  private readonly msal = inject(MsalService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    this.msal.handleRedirectObservable().subscribe({
      next: (result) => {
        const account =
          result?.account ?? this.msal.instance.getAllAccounts()[0] ?? null;
        if (account) {
          this.msal.instance.setActiveAccount(account);
          this.router.navigate(['/dashboard']);
        } else {
          this.router.navigate(['/login']);
        }
      },
      error: () => this.router.navigate(['/login']),
    });
  }
}
