import { useMutation, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export function useGeocode() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ placeId, address, sessionToken }) => {
      const { data } = await axios.post(`${API_BASE}/api/maps/geocode`, {
        placeId,
        address,
        sessionToken,
      });
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['history'] });
    },
  });
}
