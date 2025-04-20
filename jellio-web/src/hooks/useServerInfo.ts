import { useEffect, useState } from 'react';
import useAccessToken from '@/hooks/useAccessToken.ts';
import { getServerInfo } from '@/services/backendService.ts';

const useServerInfo = (): any | null | undefined => {
  const accessToken = useAccessToken();
  const [serverInfo, setServerInfo] = useState<any | null | undefined>();

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
