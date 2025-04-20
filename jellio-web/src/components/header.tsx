import { FC } from 'react';
import { Icons } from '@/components/icons.tsx';
import { ThemeToggle } from '@/components/themeToggle.tsx';
import { Button } from '@/components/ui/button.tsx';

const Header: FC = () => {
  return (
    <div className="flex h-12 items-center">
      <Button variant="ghost" size="icon">
        <a href="https://github.com/vanchaxy/jellio">
          <Icons.gitHub className="h-5 w-5" />
        </a>
      </Button>
      <Button variant="ghost" size="icon">
        <a href="https://discord.gg/8RWUkebmDs">
          <Icons.discord className="h-5 w-5" />
        </a>
      </Button>
      <Button variant="ghost" size="icon">
        <a href="mailto:support@jellio.stream">
          <Icons.mail className="h-5 w-5" />
        </a>
      </Button>
      <div className="flex flex-1 items-center justify-end">
        <ThemeToggle />
      </div>
    </div>
  );
};

export default Header;
