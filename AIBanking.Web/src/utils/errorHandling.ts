type AxiosLike = { response?: { data?: { message?: string } } };

export function getAxiosErrorMessage(err: unknown, fallback: string): string {
  return (err as AxiosLike)?.response?.data?.message ?? fallback;
}
