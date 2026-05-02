import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { RedirectRequest } from '@azure/msal-browser';

import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="page">
      <h1>Sign in</h1>
      <p>Redirecting to the sign-in page...</p>
      <button type="button" (click)="login()">Continue</button>
    </section>
  `,
  styles: [
    `
      .page {
        padding: 2rem;
        max-width: 480px;
        margin: 0 auto;
        text-align: center;
      }
      button {
        background: #2563eb;
        color: #fff;
        border: 0;
        padding: 0.5rem 1.25rem;
        border-radius: 4px;
        cursor: pointer;
      }
    `,
  ],
})
export class LoginComponent implements OnInit {
  private readonly msal = inject(MsalService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    const accounts = this.msal.instance.getAllAccounts();
    if (accounts.length > 0) {
      this.router.navigate(['/dashboard']);
      return;
    }
    this.login();
  }

  login(): void {
    const request: RedirectRequest = { scopes: environment.authScopes };
    this.msal.loginRedirect(request);
  }
}
