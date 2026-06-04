export function DateFilter({ dateFrom, dateTo, onChange }) {
  return (
    <fieldset className="flex flex-wrap items-center gap-4 border-0 p-0 m-0">
      <legend className="text-sm font-medium text-gray-600 float-left mr-4">Filtrar por fecha:</legend>
      <div className="flex items-center gap-2">
        <label htmlFor="date-from" className="text-sm text-gray-600">Desde</label>
        <input
          id="date-from"
          type="date"
          value={dateFrom}
          onChange={(e) => onChange({ dateFrom: e.target.value, dateTo })}
          className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg
                     focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div className="flex items-center gap-2">
        <label htmlFor="date-to" className="text-sm text-gray-600">Hasta</label>
        <input
          id="date-to"
          type="date"
          value={dateTo}
          min={dateFrom}
          onChange={(e) => onChange({ dateFrom, dateTo: e.target.value })}
          className="px-3 py-1.5 text-sm border border-gray-300 rounded-lg
                     focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      {(dateFrom || dateTo) && (
        <button
          onClick={() => onChange({ dateFrom: '', dateTo: '' })}
          aria-label="Limpiar filtro de fechas"
          className="text-sm text-blue-600 hover:underline"
        >
          Limpiar
        </button>
      )}
    </fieldset>
  );
}
