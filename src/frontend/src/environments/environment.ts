export const environment = {
  production: false,
  apiUrl: 'http://localhost:7071/api',
  authAuthority:
    'https://WardRanch.ciamlogin.com/6476505d-d82a-41d7-bc66-a8b7cea00459',
  authAuthorityDomain: 'WardRanch.ciamlogin.com',
  authClientId: '70e15efc-68c4-4bab-b60f-325b1f6063b1',
  authScopes: [
    'api://e33b40d1-72b6-4616-9294-e7ee7b4fd053/access_as_user',
  ] as string[],
  authRedirectUri: 'http://localhost:4200/callback',
  authPostLogoutRedirectUri: 'http://localhost:4200/',
};
