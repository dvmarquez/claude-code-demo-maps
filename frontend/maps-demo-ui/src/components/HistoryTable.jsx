import dayjs from 'dayjs';

export function HistoryTable({ data, page, totalPages, totalCount, isLoading, onPageChange }) {
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-10 bg-gray-100 rounded animate-pulse" />
        ))}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <p className="text-center text-gray-500 py-8">No hay consultas registradas.</p>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">{totalCount} consulta{totalCount !== 1 ? 's' : ''} en total</p>
      </div>

      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="w-full text-sm text-left">
          <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
            <tr>
              <th className="px-4 py-3">Dirección</th>
              <th className="px-4 py-3 text-right">Latitud</th>
              <th className="px-4 py-3 text-right">Longitud</th>
              <th className="px-4 py-3 text-right">Fecha y hora</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {data.map((item) => (
              <tr key={item.id} className="hover:bg-gray-50 transition-colors">
                <td className="px-4 py-3 text-gray-800 max-w-xs truncate">{item.address}</td>
                <td className="px-4 py-3 text-right font-mono text-gray-600">
                  {Number(item.latitude).toFixed(6)}
                </td>
                <td className="px-4 py-3 text-right font-mono text-gray-600">
                  {Number(item.longitude).toFixed(6)}
                </td>
                <td className="px-4 py-3 text-right text-gray-500 whitespace-nowrap">
                  {dayjs(item.queriedAt).format('DD/MM/YYYY HH:mm')}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between">
        <button
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300
                     rounded-lg hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Anterior
        </button>
        <span className="text-sm text-gray-600">
          Página {page} de {totalPages}
        </span>
        <button
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300
                     rounded-lg hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Siguiente
        </button>
      </div>
    </div>
  );
}
