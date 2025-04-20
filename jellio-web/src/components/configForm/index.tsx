import type { FC } from 'react';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { encode } from 'js-base64';
import { Clipboard, Globe, Monitor } from 'lucide-react';
import { useForm } from 'react-hook-form';
import {
  LibrariesField,
  ServerNameField,
} from '@/components/configForm/fields';
import type { ConfigFormType } from '@/components/configForm/formSchema.tsx';
import { formSchema } from '@/components/configForm/formSchema.tsx';
import { Button } from '@/components/ui/button.tsx';
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from '@/components/ui/dropdown-menu';
import { Form } from '@/components/ui/form';
import { getBaseUrl } from '@/lib/utils';
import { startAddonSession } from '@/services/backendService';
import type { ServerInfo } from '@/types';

interface Props {
  serverInfo: ServerInfo;
}

const ConfigForm: FC<Props> = ({ serverInfo }) => {
  const [copied, setCopied] = useState(false);
  const form = useForm<ConfigFormType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      serverName: serverInfo.serverName,
      libraries: [],
    },
  });

  const serverName = form.watch('serverName');

  const isHttps = window.location.protocol === 'https:';

  const handleInstall = async (action: 'clipboard' | 'web' | 'client') => {
    const values = form.getValues();
    const newToken = await startAddonSession(serverInfo.accessToken);
    const configuration = {
      AuthToken: newToken,
      LibrariesGuids: values.libraries.map((lib) =>
        lib.key.replace(/^(.{8})(.{4})(.{4})(.{4})(.{12})$/, '$1-$2-$3-$4-$5'),
      ),
      ServerName: serverInfo.serverName,
    };
    const encodedConfiguration = encode(JSON.stringify(configuration), true);
    const addonUrl = `${getBaseUrl()}/${encodedConfiguration}/manifest.json`;
    if (action === 'clipboard') {
      navigator.clipboard.writeText(addonUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 1000);
    } else if (action === 'web') {
      window.location.href = `https://web.stremio.com/#/addons?addon=${encodeURIComponent(addonUrl)}`;
    } else {
      window.location.href = addonUrl.replace(/https?:\/\//, 'stremio://');
    }
  };

  return (
    <Form {...form}>
      <form className="space-y-2 p-2 rounded-lg border">
        <ServerNameField form={form} />
        <LibrariesField
          form={form}
          serverName={serverName}
          libraries={serverInfo.libraries}
        />
        <div className="flex flex-col items-center justify-center p-3">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                className="h-11 rounded-md px-8 text-xl"
                disabled={copied}
              >
                {copied ? 'Copied' : 'Install'}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="center">
              <DropdownMenuItem
                onSelect={() => void handleInstall('clipboard')}
              >
                <Clipboard className="mr-2 h-4 w-4" />
                Copy install URL
              </DropdownMenuItem>
              <DropdownMenuItem onSelect={() => void handleInstall('web')}>
                <Globe className="mr-2 h-4 w-4" />
                Install in Stremio web
              </DropdownMenuItem>
              {isHttps && (
                <DropdownMenuItem onSelect={() => void handleInstall('client')}>
                  <Monitor className="mr-2 h-4 w-4" />
                  Install in Stremio client
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </form>
    </Form>
  );
};

export default ConfigForm;
