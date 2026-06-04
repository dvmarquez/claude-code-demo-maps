import { useState } from 'react';
import { SearchBox } from '../components/SearchBox';
import { MapPortlet } from '../components/MapPortlet';
import { DateFilter } from '../components/DateFilter';
import { HistoryTable } from '../components/HistoryTable';
import { useHistory } from '../hooks/useHistory';

export function HomePage() {
  const [currentResult, setCurrentResult] = useState(null);
  const [page, setPage] = useState(1);
  const [dateFilter, setDateFilter] = useState({ dateFrom: '', dateTo: '' });

  const { data: historyData, isLoading } = useHistory({
    page,
    pageSize: 10,
    dateFrom: dateFilter.dateFrom,
    dateTo: dateFilter.dateTo,
  });

  const handleGeocodeSuccess = (result) => {
    setCurrentResult(result);
    setPage(1);
  };

  const handleDateChange = (filter) => {
    setDateFilter(filter);
    setPage(1);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50
                   focus:px-4 focus:py-2 focus:bg-blue-600 focus:text-white focus:rounded-lg focus:shadow-lg"
      >
        Saltar al contenido
      </a>

      <header className="bg-white border-b border-gray-200 px-6 py-4">
        <h1 className="text-2xl font-semibold text-gray-900">Maps Demo</h1>
        <p className="text-sm text-gray-600">Busca una dirección para obtener sus coordenadas</p>
      </header>

      <main id="main-content" className="max-w-5xl mx-auto px-6 py-6 space-y-6">
        <section aria-labelledby="search-heading" className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 space-y-4">
          <h2 id="search-heading" className="text-base font-medium text-gray-700">Buscar dirección</h2>
          <SearchBox onGeocodeSuccess={handleGeocodeSuccess} />
          <div aria-live="polite" aria-atomic="true">
            {currentResult && (
              <div className="flex gap-6 text-sm text-gray-600">
                <span><span className="font-medium">Latitud:</span> {currentResult.latitude}</span>
                <span><span className="font-medium">Longitud:</span> {currentResult.longitude}</span>
              </div>
            )}
          </div>
        </section>

        <section aria-labelledby="map-heading" className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
          <h2 id="map-heading" className="text-base font-medium text-gray-700 mb-4">Mapa</h2>
          <MapPortlet
            latitude={currentResult?.latitude}
            longitude={currentResult?.longitude}
            address={currentResult?.address}
          />
        </section>

        <section aria-labelledby="history-heading" className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <h2 id="history-heading" className="text-base font-medium text-gray-700">Historial de consultas</h2>
            <DateFilter
              dateFrom={dateFilter.dateFrom}
              dateTo={dateFilter.dateTo}
              onChange={handleDateChange}
            />
          </div>
          <HistoryTable
            data={historyData?.data}
            page={page}
            totalPages={historyData?.totalPages ?? 1}
            totalCount={historyData?.totalCount ?? 0}
            isLoading={isLoading}
            onPageChange={setPage}
          />
        </section>
      </main>
    </div>
  );
}
