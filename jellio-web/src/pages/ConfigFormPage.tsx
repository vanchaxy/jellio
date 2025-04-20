import { FC } from 'react';
import ConfigForm from '@/components/configForm';
import FAQ from '@/components/faq.tsx';
import Header from '@/components/header.tsx';

interface Props {
  serverInfo: any;
}

const ConfigFormPage: FC<Props> = ({ serverInfo }) => {
  return (
    <div className="mx-auto max-w-2xl">
      <Header />
      <ConfigForm serverInfo={serverInfo} />
      <FAQ />
    </div>
  );
};

export default ConfigFormPage;
