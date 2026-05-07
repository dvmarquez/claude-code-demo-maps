import { useState, useRef } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { useAutocomplete } from '../hooks/useAutocomplete';
import { useGeocode } from '../hooks/useGeocode';

export function SearchBox({ onGeocodeSuccess }) {
  const [input, setInput] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);
  const sessionTokenRef = useRef(null);

  const { data: suggestions = [], isLoading: isLoadingAutocomplete } = useAutocomplete(
    input,
    sessionTokenRef.current
  );
  const { mutate: geocode, isPending: isGeocoding, error: geocodeError } = useGeocode();

  const handleChange = (e) => {
    if (!sessionTokenRef.current) sessionTokenRef.current = uuidv4();
    setInput(e.target.value);
    setShowDropdown(true);
  };

  const handleSelect = (suggestion) => {
    geocode(
      { placeId: suggestion.placeId, address: suggestion.description, sessionToken: sessionTokenRef.current },
      {
        onSuccess: (result) => {
          setInput('');
          setShowDropdown(false);
          sessionTokenRef.current = null;
          onGeocodeSuccess?.(result);
        },
      }
    );
  };

  const handleBlur = () => setTimeout(() => setShowDropdown(false), 150);

  return (
    <div className="relative w-full">
      <div className="flex items-center gap-2">
        <input
          type="text"
          value={input}
          onChange={handleChange}
          onFocus={() => input.length >= 3 && setShowDropdown(true)}
          onBlur={handleBlur}
          placeholder="Buscar dirección..."
          disabled={isGeocoding}
          className="w-full px-4 py-3 rounded-lg border border-gray-300 shadow-sm text-base
                     focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                     disabled:bg-gray-100 disabled:cursor-wait"
        />
        {(isLoadingAutocomplete || isGeocoding) && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2">
            <div className="w-5 h-5 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
          </div>
        )}
      </div>

      {geocodeError && (
        <p className="mt-1 text-sm text-red-600">
          Error al obtener coordenadas. Intente de nuevo.
        </p>
      )}

      {showDropdown && suggestions.length > 0 && (
        <ul className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg overflow-hidden">
          {suggestions.map((s) => (
            <li
              key={s.placeId}
              onMouseDown={() => handleSelect(s)}
              className="px-4 py-3 text-sm text-gray-800 cursor-pointer hover:bg-blue-50
                         border-b border-gray-100 last:border-0"
            >
              {s.description}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
