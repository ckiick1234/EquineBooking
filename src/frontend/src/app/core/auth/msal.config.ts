import {
  BrowserCacheLocation,
  IPublicClientApplication,
  InteractionType,
  LogLevel,
  PublicClientApplication,
} from '@azure/msal-browser';
import {
  MsalGuardConfiguration,
  MsalInterceptorConfiguration,
} from '@azure/msal-angular';

import { environment } from '../../../environments/environment';

export function msalInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: environment.authClientId,
      authority: environment.authAuthority,
      knownAuthorities: [environment.authAuthorityDomain],
      redirectUri: environment.authRedirectUri,
      postLogoutRedirectUri: environment.authPostLogoutRedirectUri,
    },
    cache: {
      cacheLocation: BrowserCacheLocation.LocalStorage,
    },
    system: {
      loggerOptions: {
        loggerCallback: (level, message) => {
          if (!environment.production) {
            console.log(`[MSAL ${LogLevel[level]}] ${message}`);
          }
        },
        logLevel: environment.production ? LogLevel.Warning : LogLevel.Info,
        piiLoggingEnabled: false,
      },
    },
  });
}

export function msalGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: { scopes: environment.authScopes },
    loginFailedRoute: '/login',
  };
}

export function msalInterceptorConfigFactory(): MsalInterceptorConfiguration {
  const protectedResourceMap = new Map<string, string[] | null>([
    [`${environment.apiUrl}/*`, environment.authScopes],
  ]);

  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap,
  };
}
