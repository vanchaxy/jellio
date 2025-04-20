import { useEffect, useState } from 'react';
import type { Maybe } from '@/types';

const useAccessToken = (): Maybe<string> => {
  const [accessToken, setAccessToken] = useState<Maybe<string>>();

  useEffect(() => {
    const storedCredentialsString = localStorage.getItem(
      'jellyfin_credentials',
    );

    if (storedCredentialsString) {
      const storedCredentials = JSON.parse(storedCredentialsString) as {
        Servers: { AccessToken: string }[];
      };
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
