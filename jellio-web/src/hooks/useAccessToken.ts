import { useEffect, useState } from 'react';

const useAccessToken = (): string | null | undefined => {
  const [accessToken, setAccessToken] = useState<string | null | undefined>();

  useEffect(() => {
    const storedCredentialsString = localStorage.getItem(
      'jellyfin_credentials',
    );

    if (storedCredentialsString) {
      const storedCredentials = JSON.parse(storedCredentialsString);
      const serverCredentials = storedCredentials.Servers.find(
        (e: any) => e.AccessToken,
      );
      if (serverCredentials) {
        setAccessToken(serverCredentials.AccessToken);
        return;
      }
    }
    setAccessToken(null);
  }, []);

  return accessToken;
};

export default useAccessToken;
