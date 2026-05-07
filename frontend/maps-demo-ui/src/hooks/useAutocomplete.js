import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import { useDebounce } from './useDebounce';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export function useAutocomplete(input, sessionToken) {
  const debouncedInput = useDebounce(input, 300);

  return useQuery({
    queryKey: ['autocomplete', debouncedInput, sessionToken],
    queryFn: async () => {
      const { data } = await axios.post(`${API_BASE}/api/maps/autocomplete`, {
        input: debouncedInput,
        sessionToken,
      });
      return data;
    },
    enabled: !!debouncedInput && debouncedInput.trim().length >= 3,
    staleTime: 30_000,
  });
}
