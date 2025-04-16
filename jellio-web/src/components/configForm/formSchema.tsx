import { z } from 'zod';

export const formSchema = z.object({
  serverName: z.string(),
  libraries: z.array(
    z.object({
      key: z.string(),
      name: z.string(),
      type: z.string(),
    }),
  ),
});

export type ConfigFormType = z.infer<typeof formSchema>;
