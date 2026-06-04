import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import dayjs from 'dayjs';

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

export function useHistory({ page, pageSize, dateFrom, dateTo }) {
  return useQuery({
    queryKey: ['history', page, pageSize, dateFrom, dateTo],
    queryFn: async () => {
      const params = { page, pageSize };
      if (dateFrom) params.dateFrom = dayjs(dateFrom).toISOString();
      if (dateTo) params.dateTo = dayjs(dateTo).toISOString();
      const { data } = await axios.get(`${API_BASE}/api/maps/history`, { params });
      return data;
    },
  });
}
