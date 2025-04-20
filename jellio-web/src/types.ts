export interface Library {
  name: string;
  key: string;
  type: string;
}

export interface ServerInfo {
  serverName: string;
  accessToken: string;
  libraries: Library[];
}

export type Maybe<T> = T | null | undefined;
