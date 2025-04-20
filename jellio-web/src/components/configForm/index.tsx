import { FC } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { encode } from 'js-base64';
import { useForm } from 'react-hook-form';
import {
  LibrariesField,
  ServerNameField,
} from '@/components/configForm/fields';
import {
  formSchema,
  ConfigFormType,
} from '@/components/configForm/formSchema.tsx';
import { Icons } from '@/components/icons';
import { Button } from '@/components/ui/button.tsx';
import { Form } from '@/components/ui/form';
import { getBaseUrl } from '@/lib/utils';
import { startAddonSession } from '@/services/backendService';

interface Props {
  serverInfo: any;
}

const ConfigForm: FC<Props> = ({ serverInfo }) => {
  const form = useForm<ConfigFormType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      serverName: serverInfo.serverName,
      libraries: [],
    },
  });

  const serverName = form.watch('serverName');

  async function onSubmit(configuration: any, event: any) {
    const newToken = await startAddonSession(serverInfo.accessToken);
    var configuration: any = {
      AuthToken: newToken,
      LibrariesGuids: configuration.libraries.map((lib: any) =>
        lib.key.replace(/^(.{8})(.{4})(.{4})(.{4})(.{12})$/, '$1-$2-$3-$4-$5'),
      ),
      ServerName: serverInfo.serverName,
    };
    console.log(configuration);

    const encodedConfiguration = encode(JSON.stringify(configuration), true);
    const addonUrl = `${getBaseUrl()}/${encodedConfiguration}/manifest.json`;

    if (event.nativeEvent.submitter.name === 'clipboard') {
      navigator.clipboard.writeText(addonUrl);
    } else {
      window.location.href = addonUrl.replace(/https?:\/\//, 'stremio://');
    }
  }

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-2 p-2 rounded-lg border"
      >
        <ServerNameField form={form} />
        <LibrariesField
          form={form}
          serverName={serverName}
          libraries={serverInfo.libraries}
        />
        <div className="flex items-center space-x-1 justify-center p-3">
          <Button className="h-11 w-10 p-2" type="submit" name="clipboard">
            <Icons.clipboard />
          </Button>
          <Button
            className="h-11 rounded-md px-8 text-xl"
            type="submit"
            name="install"
          >
            Install
          </Button>
        </div>
      </form>
    </Form>
  );
};

export default ConfigForm;
