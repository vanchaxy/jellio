import { useEffect, useState } from 'react';
import useAccessToken from '@/hooks/useAccessToken.ts';
import { getServerInfo } from '@/services/backendService.ts';
import type { ServerInfo, Maybe } from '@/types';

const useServerInfo = (): Maybe<ServerInfo> => {
  const accessToken = useAccessToken();
  const [serverInfo, setServerInfo] = useState<ServerInfo | null | undefined>();

  useEffect(() => {
    if (accessToken === null) {
      setServerInfo(null);
    } else if (accessToken) {
      const fetchServerInfo = async (): Promise<void> => {
        try {
          var serverInfo = await getServerInfo(accessToken);
          setServerInfo({
            accessToken: accessToken,
            ...serverInfo,
          });
        } catch {
          setServerInfo(null);
        }
      };
      void fetchServerInfo();
    }
  }, [accessToken]);

  return serverInfo;
};

export default useServerInfo;
