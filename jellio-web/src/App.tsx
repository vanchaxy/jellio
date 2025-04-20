import { ThemeProvider } from '@/components/themeProvider';
import useServerInfo from '@/hooks/useServerInfo.ts';
import { getBaseUrl } from '@/lib/utils';
import ConfigFormPage from '@/pages/ConfigFormPage';

function App() {
  const serverInfo = useServerInfo();

  if (serverInfo === undefined) {
    return;
  }

  if (serverInfo === null) {
    const jellyfinUrl = getBaseUrl().split('/jellio')[0];
    window.location.replace(
      `${jellyfinUrl}/web/#/login.html?url=%2Fconfigurationpage%3Fname%3DJellio`,
    );
    return;
  }

  return (
    <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
      <ConfigFormPage serverInfo={serverInfo} />
    </ThemeProvider>
  );
}

export default App;
