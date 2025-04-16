import axios from 'axios';
import { getBaseUrl } from '@/lib/utils';
import type { Library } from '@/types';

export const getServerInfo = async (
  token: string,
): Promise<{ serverName: string; libraries: Library[] }> => {
  try {
    const response = await axios.get(`${getBaseUrl()}/server-info`, {
      headers: {
        Authorization: `MediaBrowser Token="${token}"`,
      },
    });

    return {
      serverName: response.data.name,
      libraries: response.data.libraries.map(
        (lib: { Name: string; Id: string; CollectionType: string }) => {
          return { name: lib.Name, key: lib.Id, type: lib.CollectionType };
        },
      ),
    };
  } catch (error) {
    console.error('Error while getting server info:', error);
    throw error;
  }
};

export const startAddonSession = async (token: string): Promise<string> => {
  try {
    const response = await axios.post(`${getBaseUrl()}/start-session`, null, {
      headers: {
        Authorization: `MediaBrowser Token="${token}"`,
      },
    });
    return response.data.accessToken;
  } catch (error) {
    console.error('Error starting new session:', error);
    throw error;
  }
};
